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
        private ObservableCollection<DHMData> _dhmData;
        public DHMViewModel()
        {
            DHMData = new ObservableCollection<DHMData>();
        }
        private void ParseDHMFile()
        {
            using (var reader = new StreamReader("path\\to\\file.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    //var record = new DHMData()
                    //{
                    //     = csv.GetField<int>("Id"),
                    //    Name = csv.GetField("Name")
                    //};
                    //DHMData.Add();
                }
            }
        }
        public ObservableCollection<DHMData> DHMData
        {
            get { return _dhmData; }
            set { _dhmData = value; OnPropertyChanged(); }
        }
    }
}
