using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicoApp.Model
{
    internal class DisplacementData
    {
        double Frequency { get; set; }
        double Displacement { get; set; }
    }
    internal class DhmData
    {
        public DhmData()
        {
            Data = new ObservableCollection<DisplacementData>();
        }
        public ObservableCollection<DisplacementData> Data { get; set; }
    }
}
