using PicoApp.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using Prism.Commands;
using NationalInstruments.Visa;
using System.Windows;
using System.Windows.Media;
using System.Threading;
using Ivi.Visa;

namespace PicoApp.ViewModel
{
    internal class OscopeViewModel : ViewModelBase
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        private readonly Brush connectedColor = (Brush)Application.Current.Resources["SecondaryHueLightBrush"];
        private readonly Brush disconnectedColor = (Brush)Application.Current.Resources["PrimaryHueLightBrush"];
        private readonly string connectedString = "Connected";
        private readonly string disconnectedString = "Disconnected";
        private readonly LineSeries power;
        private IVisaAsyncResult asyncHandle = null;
        List<Task> ActiveTasks = new();
        int TotalSamples = 0;
        public OscopeViewModel()
        {
            Disconnected();
            OscopeConnect = new DelegateCommand(StartAsyncConnect);
            RefreshDevices = new DelegateCommand(StartAsyncRefresh);
            RunClick = new DelegateCommand(StartScope);
            StopClick = new DelegateCommand(StopOscope);
            Resources = new ObservableCollection<string>();
            var chart = new PlotModel { Title = "Raw Data" };
            var primaryAxis = new LinearAxis { Position = AxisPosition.Left };
            power = new LineSeries()
            {
                StrokeThickness = 2,
                MarkerSize = 4,
                MarkerStroke = OxyColors.Blue
            };
            power.Title = "";
            chart.Axes.Add(primaryAxis);
            chart.Series.Add(power);
            PicoData = new ObservableCollection<OscopeData>();
            PicoChart = chart;
        }
        private void StartAsyncConnect()
        {
            AnimateBar().Await();
            DeviceConnect().Await();
        }
        private void StartAsyncRefresh()
        {
            AnimateBar().Await();
            ScanDevices().Await();
        }
        private void StopOscope()
        {
            cts.Cancel();
            cts = new CancellationTokenSource();
            if (NiSession != null) NiSession.RawIO.AbortAsyncOperation(asyncHandle);
        }
        private async Task DeviceConnect()
        {
            if (SelectedDevice == null) return;
            try
            {
                Connecting();
                using (var rmSession = new ResourceManager())
                {
                    NiSession = (MessageBasedSession)await Task.Run(() => rmSession.Open(SelectedDevice));
                    // Use SynchronizeCallbacks to specify that the object marshals callbacks across threads appropriately.
                    NiSession.SynchronizeCallbacks = true;
                    NiSession.TimeoutMilliseconds = 1000;
                    Connected();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Disconnected();
            }
            finally
            {
                cts.Cancel();
                cts = new CancellationTokenSource();
            }
        }
        private void StartScope()
        {
            ScopeScheduler().Await();
        }
        private async Task ScopeScheduler()
        {
            SetupScope();
            RunOscopeAsync();
            await IsAcquisitionComplete();
            PicoData = new ObservableCollection<OscopeData>(await CollectMeasurements());
            UpdateGraph();
        }
        private void SetupScope()
        {
            string setupString =
                ":ACQuire:COUNt 1000;" +
                ":ACQuire:TYPE AVERage;" +
                ":TIMebase:SCALe 1e-06;" +
                ":CHANnel1:SCALe 2 V;" +
                ":CHANnel2:SCALe 500 mV;" +
                ":TRIGger:EDGE:SOURce CHANNEL;" +
                "WAVeform:FORMat ASCii;" +
                ":WAVeform:POINts 10000;" +
                ":TRIGger:EDGE:LEVel 1,CHANNEL1;";
            Write(setupString);
        }
        private void RunOscopeAsync()
        {
            TotalSamples = AutotuneCount * NumSamples;
            string cmd = "ACQuire:SEGMented:COUNt " + (AutotuneCount * NumSamples).ToString() + "\n" +
                ":TRIGger:MODE DELay\n" +
                ":TRIGger:DELay:TRIGger:SOURce CHANNEL1\n" +
                ":TRIGger:DELay:ARM:SOURce CHANNEL1\n" +
                ":ACQuire:MODE SEGMented\n" +
                ":TRIGger:DELay:TDELay:TIME " + (AutotuneInterval / 1000 / NumSamples).ToString() + "\n" +
                ":SINGle";
            Task.Run(() => Write(cmd));
        }
        private async Task IsAcquisitionComplete()
        {
            int timesTried = 0;
            int maxTimesTried = 200;
            int delayTime = 500;
            await Task.Delay(delayTime);
            await Task.Run(() =>
            {
                int operationComplete = 0;
                string cmd = "*OPC?";

                while (operationComplete == 0 && timesTried < maxTimesTried)
                {
                    try
                    {
                        timesTried++;
                        NiSession.RawIO.Write(cmd);
                        operationComplete = Convert.ToInt32(NiSession.RawIO.ReadString());
                    }
                    catch
                    {
                        // If there is an error just keep trying to send the opc query.
                    }
                }
            });

        }
        private async Task<List<OscopeData>> CollectMeasurements()
        {
            List<OscopeData> dataList = new List<OscopeData>();
            await Task.Run(() =>
            {
                string[] measurements =
            {
                ":MEASure:FREQuency? CHANNEL1",
                ":MEASure:VRMS? DISPlay,AC,CHANNEL1",
                ":MEASure:VRMS? DISPlay,AC,CHANNEL2",
                ":MEASure:PHASe? CHANnel1,CHANnel2"
            };
                string cmd;
                List<string> strings = new();
                List<double> frequencies = new();
                List<double> voltages = new();
                List<double> currents = new();
                List<double> phases = new();
                OscopeData oscopeData;
                int index = 0;
                try
                {
                    for (int i = 1; i <= TotalSamples; i++)
                    {
                        cmd = ":ACQuire:SEGMented:INDex " + i.ToString();
                        NiSession.RawIO.Write(cmd);
                        foreach (string command in measurements)
                        {
                            cmd = command;
                            NiSession.RawIO.Write(cmd);
                            strings.Add(NiSession.RawIO.ReadString());
                        }
                    }
                    for (int i = 0; i < AutotuneCount; i++)
                    {
                        oscopeData = new OscopeData();
                        for (int j = 0; j < NumSamples; j++)
                        {
                            for (int k = 0; k < measurements.Count(); k++)
                            {
                                if (k == 0)
                                {
                                    frequencies.Add(Double.Parse(strings[index]));
                                }
                                else if (k == 1)
                                {
                                    voltages.Add(Double.Parse(strings[index]));
                                }
                                else if (k == 2)
                                {
                                    currents.Add(Double.Parse(strings[index]));
                                }
                                else if (k == 3)
                                {
                                    phases.Add(Double.Parse(strings[index]));
                                }
                                index++;
                            }
                        }
                        oscopeData.Frequency = frequencies.Average();
                        oscopeData.VoltageRMS = voltages.Average();
                        oscopeData.CurrentRMS = currents.Average();
                        oscopeData.Phase = phases.Average();
                        oscopeData.CalcPower();
                        dataList.Add(oscopeData);
                        frequencies.Clear();
                        voltages.Clear();
                        currents.Clear();
                        phases.Clear();
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
            return dataList;
        }
        /// <summary>
        /// The inputted callback function is invoked when the command is returned.
        /// </summary>
        /// <param name="f"></param>
        private void Write(string cmd)
        {
            if (string.IsNullOrEmpty(cmd)) throw new Exception("Command is null.");
            try
            {
                NiSession.RawIO.Write(cmd);
            }
            catch (Exception exp)
            {
                if (exp.Message == "Unable to queue the asynchronous operation because there is already an operation in progress.") return;
                MessageBox.Show(exp.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateGraph()
        {
            power.Points.Clear();
            foreach (var data in PicoData)
            {
                power.Points.Add(new DataPoint(data.Frequency, data.Power));
            }
            PicoChart.InvalidatePlot(true);
        }
        private void UpdateStatus()
        {
            if (NiSession == null) Disconnected();
        }
        private void Connecting()
        {
            StatusColor = disconnectedColor;
            ConnectEnabled = false;
            RefreshEnabled = false;
            ProgressVisibility = "Visible";
            ConnectionStatus = "Connecting...";
        }
        private void Refreshing()
        {
            ProgressVisibility = "Visible";
            ConnectEnabled = false;
            RefreshEnabled = false;
            ConnectionStatus = "Refreshing...";
        }
        private void Connected()
        {
            ProgressVisibility = "Collapsed";
            ConnectEnabled = true;
            RefreshEnabled = true;
            ConnectionStatus = connectedString;
            StatusColor = connectedColor;
        }
        private void Disconnected()
        {
            ProgressVisibility = "Collapsed";
            StatusColor = disconnectedColor;
            RefreshEnabled = true;
            ConnectEnabled = false;
            ConnectionStatus = disconnectedString;
        }
        private async Task ScanDevices()
        {
            var curStatus = ConnectionStatus;
            Refreshing();
            try
            {
                Resources.Clear();
                using (var rmSession = new ResourceManager())
                {
                    var devices = await Task.Run(() => rmSession.Find("(ASRL|GPIB|TCPIP|USB)?*INSTR"));
                    foreach (string s in devices)
                    {
                        Resources.Add(s);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                cts.Cancel();
                cts = new CancellationTokenSource();
                UpdateStatus();
            }
        }
        public async Task AnimateBar()
        {
            while (!cts.Token.IsCancellationRequested)
            {
                ProgressBarValue += 5;
                await Task.Delay(200);
            }
            cts.Dispose();
            cts = new CancellationTokenSource();
        }
        public DelegateCommand OscopeConnect { get; set; }
        public DelegateCommand RefreshDevices { get; set; }
        public DelegateCommand RunClick { get; set; }
        public DelegateCommand StopClick { get; set; }
        private bool runEnable;
        public bool RunEnable
        {
            get
            {
                return runEnable;
            }
            set
            {
                runEnable = value;
                OnPropertyChanged();
            }
        }
        private PlotModel picoChart;
        public PlotModel PicoChart
        {
            get { return picoChart; }
            set 
            { 
                picoChart = value; 
                OnPropertyChanged(); 
            }
        }
        private ObservableCollection<OscopeData> picoData;
        public ObservableCollection<OscopeData> PicoData
        {
            get { return picoData; }
            set { picoData = value; OnPropertyChanged(); }
        }
        private OscopeData selectedPicoData;
        public OscopeData SelectedPicoData
        {
            get
            {
                return selectedPicoData;
            }
            set
            {
                selectedPicoData = value;
                OnPropertyChanged();
            }
        }
        private ObservableCollection<string> resources;
        public ObservableCollection<string> Resources
        {
            get { return resources; }
            set { resources = value; OnPropertyChanged(); }
        }
        private string selectedDevice;
        public string SelectedDevice { 
            get 
            { 
                return selectedDevice; 
            } 
            set 
            { 
                selectedDevice = value;
                if (selectedDevice == null) ConnectEnabled = false;
                else ConnectEnabled = true;
                OnPropertyChanged(); 
            } 
        }
        private bool refreshEnabled;
        public bool RefreshEnabled
        {
            get { return refreshEnabled; }
            set { refreshEnabled = value; OnPropertyChanged(); }
        }
        private bool connectEnabled;
        public bool ConnectEnabled
        {
            get { return connectEnabled; }
            set { connectEnabled = value; OnPropertyChanged(); }
        }
        private string connectionStatus;
        public string ConnectionStatus
        {
            get { return connectionStatus; }
            set { connectionStatus = value; OnPropertyChanged(); }
        }
        private Brush statusColor;
        public Brush StatusColor
        {
            get { return statusColor; }
            set { statusColor = value; OnPropertyChanged(); }
        }
        private Brush connectBorderColor;
        public Brush ConnectBorderColor
        {
            get { return connectBorderColor; }
            set { connectBorderColor = value; OnPropertyChanged(); }
        }
        private Brush disconnectBorderColor;
        public Brush DisconnectBorderColor
        {
            get { return disconnectBorderColor; }
            set { disconnectBorderColor = value; OnPropertyChanged(); }
        }
        private int progressBarValue;
        public int ProgressBarValue
        {
            get { return progressBarValue; }
            set 
            {
                if (progressBarValue == value) return;
                if (value > 100 || value < 0) progressBarValue = 0;
                else progressBarValue = value; 
                OnPropertyChanged(); 
            }
        }
        private string progressVisibility;
        public string ProgressVisibility
        {
            get { return progressVisibility; }
            set { progressVisibility = value; OnPropertyChanged(); }
        }
        private MessageBasedSession niSession;
        public MessageBasedSession NiSession
        {
            get
            {
                return niSession;
            }
            set
            {
                niSession = value;
                if (niSession == null)
                {
                    RunEnable = false;
                }
                else
                {
                    RunEnable = true;
                }
            }
        }
        private int autotuneCount;
        public int AutotuneCount
        {
            get { return autotuneCount; }
            set { autotuneCount = value; OnPropertyChanged(); }
        }
        private double autotuneInterval;
        public double AutotuneInterval
        {
            get
            {
                return autotuneInterval;
            }
            set
            {
                autotuneInterval = value;
                OnPropertyChanged();
            }
        }
        private int numSamples;
        public int NumSamples
        {
            get { return numSamples; }
            set { numSamples = value; OnPropertyChanged(); }
        }
    }
}
