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

namespace PicoApp.ViewModel
{
    internal class OscopeViewModel : ViewModelBase
    {
        private readonly LineSeries current;
        private readonly LineSeries voltage;
        public MessageBasedSession NiSession { get; set; }
        public OscopeViewModel()
        {
            OscopeConnect = new DelegateCommand(DeviceConnect);
            RefreshDevices = new DelegateCommand(ScanDevices);
            Resources = new ObservableCollection<string>();
            var chart = new PlotModel { Title = "Raw Data" };
            var primaryAxis = new LinearAxis { Position = AxisPosition.Left };
            chart.Axes.Add(primaryAxis);
            var secondaryAxis = new LinearAxis { Position = AxisPosition.Right };
            chart.Axes.Add(secondaryAxis);
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
        private void DeviceConnect()
        {
            if (SelectedDevice == null) return;
            try
            {
                using (var rmSession = new ResourceManager())
                {
                    NiSession = (MessageBasedSession)rmSession.Open(SelectedDevice);
                    // Use SynchronizeCallbacks to specify that the object marshals callbacks across threads appropriately.
                    NiSession.SynchronizeCallbacks = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        private void ScanDevices()
        {
            Resources.Clear();
            using (var rmSession = new ResourceManager())
            {
                foreach (string s in rmSession.Find("(ASRL|GPIB|TCPIP|USB)?*INSTR"))
                {
                    Resources.Add(s);
                }
            }   
        }
        public DelegateCommand OscopeConnect { get; set; }
        public DelegateCommand RefreshDevices { get; set; }
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
                foreach (var data in selectedPicoData.RawData)
                {
                    current.Points.Add(new DataPoint(data.Time, data.Current));
                    voltage.Points.Add(new DataPoint(data.Time, data.Voltage));
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
                OnPropertyChanged(); 
            } 
        }
    }
}
