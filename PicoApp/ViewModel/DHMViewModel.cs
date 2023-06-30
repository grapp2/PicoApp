using CsvHelper;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using PicoApp.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PicoApp.ViewModel
{
    internal class DHMViewModel : ViewModelBase
    {
        private ObservableCollection<DhmData> groupedData;
        private PlotModel plotModel;

        public DHMViewModel()
        {
            GroupedData = new ObservableCollection<DhmData>();
            RawData = new List<DhmData>();
            // Create the graph
            PlotModel = new PlotModel { Title = "DHM Data" };
            var primaryAxis = new LinearAxis { Position = AxisPosition.Left };
            PlotModel.Axes.Add(primaryAxis);
            primaryAxis.Title = "Displacement";
            var secondaryAxis = new LinearAxis { Position = AxisPosition.Right };
            PlotModel.Axes.Add(secondaryAxis);

            GroupedLineSeries = new LineSeries()
            {
                StrokeThickness = 2,
                MarkerSize = 4,
                MarkerStroke = OxyColors.Blue
            };
            ParseDHMFile();
            GeneratePlot();

            UploadCommand = new RelayCommand(UploadFile);
        }

        private void GeneratePlot()
        {
            var groupData = GroupedData.FirstOrDefault();
            if (groupData != null)
            {
                // Iterate through the grouped data and add it to the plot
                foreach (var data in GroupedData)
                {
                    GroupedLineSeries.Points.Add(new DataPoint(data.Frequency, data.Displacement));
                }
            }
            // Add the displacement series to the plot model
            PlotModel.Series.Add(GroupedLineSeries);
            PlotModel.InvalidatePlot(true);
        }

        private void UploadFile()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                // Call the method to parse the uploaded file
                ParseDHMFile(filePath);

                // Update the plot model and regenerate the plot
                GeneratePlot();
            }
        }

        private void ParseDHMFile(string filePath)
        {
            try
            {
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                    {
                        var record = new DhmData()
                        {
                            Frequency = csv.GetField<double>(" Frequency[Hz]"),
                            Displacement = csv.GetField<double>(" Region4[nm]")
                        };

                        RawData.Add(record);
                    }
                    GroupedData = new ObservableCollection<DhmData>(GroupData(RawData));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        public List<DhmData> GroupData(List<DhmData> rawData)
        {
            List<DhmData> groupedData = new List<DhmData>();
            double min = double.MaxValue;
            double max = 0;
            double lastFreq = rawData[0].Frequency;

            foreach (var data in rawData)
            {
                if (data.Frequency != lastFreq)
                {
                    double displacement = max - min;
                    var groupDisplacement = new DhmData();
                    groupDisplacement.Frequency = lastFreq;
                    groupDisplacement.Displacement = displacement;
                    groupedData.Add(groupDisplacement);
                    lastFreq = data.Frequency;
                    min = double.MaxValue;
                    max = 0;
                }

                if (data.Displacement > max)
                {
                    max = data.Displacement;
                }

                if (data.Displacement < min)
                {
                    min = data.Displacement;
                }
            }
            return groupedData;
        }
        public ObservableCollection<DhmData> GroupedData
        {
            get { return groupedData; }
            set { groupedData = value; OnPropertyChanged(); }
        }
        private DhmData selectedData;
        public DhmData SelectedData
        {
            get
            {
                return selectedData;
            }
            set
            {
                selectedData = value; OnPropertyChanged();
            }
        }
        private List<DhmData> rawData;
        public List<DhmData> RawData
        {
            get
            {
                return rawData;
            }
            set {  rawData = value; OnPropertyChanged(); }
        }
        public PlotModel PlotModel
        {
            get { return plotModel; }
            set { plotModel = value; OnPropertyChanged(); }
        }

        public LineSeries GroupedLineSeries { get; set; }
    }
}