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
        private DhmData dhmData;
        public DHMViewModel()
        {
            DHMData = new DhmData();
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
        public DhmData DHMData
        {
            get { return dhmData; }
            set { dhmData = value; OnPropertyChanged(); }
        }
    }
}
