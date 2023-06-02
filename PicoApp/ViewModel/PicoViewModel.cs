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


namespace PicoApp.ViewModel
{
    internal class PicoViewModel : ViewModelBase
    {
        LineSeries current, voltage;
        public PicoViewModel()
        {
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

            PicoData = new ObservableCollection<PicoData>();
            for (int i = 0; i < 10; i++)
            {
                var data = new PicoData();
                data.GenerateSample();
                PicoData.Add(data);
            }
            PicoChart = chart;
            SelectedPicoData = PicoData[0];
            RunPico = new DelegateCommand(FreqDetect);
        }

        public DelegateCommand RunPico { get; set; }
        private void FreqDetect()
        {

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
        private ObservableCollection<PicoData>? picoData;
        public ObservableCollection<PicoData> PicoData
        {
            get { return picoData; }
            set { picoData = value; OnPropertyChanged(); }
        }
        private PicoData? selectedPicoData;
        public PicoData SelectedPicoData
        {
            get
            {
                return selectedPicoData;
            }
            set
            {
                selectedPicoData = value;
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
    }
}
