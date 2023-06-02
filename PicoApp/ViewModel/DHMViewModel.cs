using CsvHelper;
using PicoApp.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicoApp.ViewModel
{
    internal class DHMViewModel : ViewModelBase
    {
        private ObservableCollection<GroupDhmData> dhmData;
        public DHMViewModel()
        {
            DHMData = new ObservableCollection<GroupDhmData>();
            ParseDHMFile();
        }
        private void ParseDHMFile()
        {
            GroupDhmData groupDhmData = new GroupDhmData();
            using (var reader = new StreamReader(@"C:\\Users\\JoseSalazar\\OneDrive - Pneuma Respiratory\\Project Development\\Product Development\\Template\\FrequencyDisplacement\\100hz.txt"))
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
                    groupDhmData.RawData.Add(record);
                }
            }
            DHMData.Add(groupDhmData);
        }
        public ObservableCollection<GroupDhmData> DHMData
        {
            get { return dhmData; }
            set { dhmData = value; OnPropertyChanged(); }
        }
    }
}
