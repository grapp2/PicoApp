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
        private readonly LineSeries current;
        private readonly LineSeries voltage;
        private IVisaAsyncResult asyncHandle = null;
        private Queue<string> CommandQueue = new Queue<string>();
        List<Task> ActiveTasks = new();
        double lastFreq = 0;
        public OscopeViewModel()
        {
            Disconnected();
            OscopeConnect = new DelegateCommand(StartAsyncConnect);
            RefreshDevices = new DelegateCommand(StartAsyncRefresh);
            RunClick = new DelegateCommand(QueueSetup);
            StopClick = new DelegateCommand(StopOscope);
            FrequencyThreshold = 200;
            Resources = new ObservableCollection<string>();
            var chart = new PlotModel { Title = "Raw Data" };
            var primaryAxis = new LinearAxis { Position = AxisPosition.Left };
            var secondaryAxis = new LinearAxis { Position = AxisPosition.Right };
            current = new LineSeries()
            {
                StrokeThickness = 2,
                MarkerSize = 4,
                MarkerStroke = OxyColors.Blue
            };
            voltage = new LineSeries()
            {
                StrokeThickness = 2,
                MarkerSize = 4,
                MarkerStroke = OxyColors.Blue
            };
            chart.Axes.Add(primaryAxis);
            chart.Axes.Add(secondaryAxis);
            chart.Series.Add(current);
            chart.Series.Add(voltage);
            PicoData = new ObservableCollection<OscopeData>();
            for (int i = 0; i < 10; i++)
            {
                var data = new OscopeData();
                data.GenerateSample();
                PicoData.Add(data);
            }
            PicoChart = chart;
            SelectedPicoData = PicoData[0];
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
            }
        }
        private void QueueSetup()
        {
            string setupString = ":ACQuire:COUNt 1000;" +
                                    ":ACQuire:TYPE AVERage;" +
                                    ":TIMebase:SCALe 1e-06;" +
                                    ":CHANnel1:SCALe 2 V;" +
                                    ":CHANnel2:SCALe 500 mV;" +
                                    ":TRIGger:EDGE:SOURce CHANNEL;" +
                                    "WAVeform:FORMat ASCii;" +
                                    ":WAVeform:POINts 1000;" +
                                    ":TRIGger:LEVel:HIGH 2,CHANNE;";
            StopEnable = true;
            CommandQueue.Enqueue(setupString);
            // start dequeueing the commands one at a time. Callback continues calling AsyncDequeueCommand until 
            // all setup commands have been sent.
            AsyncDequeueWrite(SetupComplete);
        }
        private async Task RunOscopeAsync()
        {
            string cmd = ":MEASure:FREQuency? CHANNEL1";
            while (!cts.Token.IsCancellationRequested)
            {
                CommandQueue.Enqueue(cmd);
                // Once the write is finished, immediately read the result.
                AsyncDequeueWrite((IVisaAsyncResult result) => AsyncRead(FrequencyReadComplete));
                await Task.Delay(1250);
            }
            cts.Dispose();
            cts = new CancellationTokenSource();
        }

        private void SetupComplete(IVisaAsyncResult result)
        {
            if (CommandQueue.Count > 0)
            {
                AsyncDequeueWrite(SetupComplete);
            }
            else
            {
                RunOscopeAsync().Await();
            }
        }
        private void FrequencyReadComplete(IVisaAsyncResult result)
        {
            MeasuredFrequency = Double.Parse(NiSession.RawIO.EndReadString(result));
            if (MeasuredFrequency > lastFreq + FrequencyThreshold || MeasuredFrequency < lastFreq - FrequencyThreshold)
            {
                FrequencyTrigger();
                lastFreq = MeasuredFrequency;
            }
        }
        private void FrequencyTrigger()
        {
           
        }
        /// <summary>
        /// The inputted callback function is invoked when the command is returned.
        /// </summary>
        /// <param name="f"></param>
        private void AsyncDequeueWrite(Action<IVisaAsyncResult> callback)
        {
            try
            {
                var cmd = CommandQueue.Dequeue();
                asyncHandle = NiSession.RawIO.BeginWrite(
                    cmd,
                    new VisaAsyncCallback(callback),
                    (object)cmd.Length);
            }
            catch (Exception exp)
            {
                if (exp.Message == "Unable to queue the asynchronous operation because there is already an operation in progress.") return;
                MessageBox.Show(exp.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AsyncRead(Action<IVisaAsyncResult> callback)
        {
            try
            {
                asyncHandle = NiSession.RawIO.BeginRead(
                    10000,
                    new VisaAsyncCallback(callback),
                    null);
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
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
        private bool stopEnable;
        public bool StopEnable
        {
            get
            {
                return stopEnable;
            }
            set
            {
                stopEnable = value;
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
                // Update graph every time selected data changes.
                current.Points.Clear();
                voltage.Points.Clear();
                int i = 0;
                foreach (var data in selectedPicoData.RawData)
                {
                    current.Points.Add(new DataPoint(i,data));
                    voltage.Points.Add(new DataPoint(i, data));
                    i++;
                }
                PicoChart.InvalidatePlot(true);
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
        private double measuredFrequency;
        public double MeasuredFrequency
        {
            get
            {
                return measuredFrequency;
            }
            set
            {
                measuredFrequency = value; 
                OnPropertyChanged();
            }
        }
        private double frequencyThreshold;
        public double FrequencyThreshold
        {
            get { return frequencyThreshold; }
            set { frequencyThreshold = value; OnPropertyChanged(); }
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
                    StopEnable = false;
                }
                else
                {
                    RunEnable = true;
                    StopEnable = false;
                }
            }
        }
    }
}
