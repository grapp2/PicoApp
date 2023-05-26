using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicoApp.Model
{
    internal class PicoData
    {
        public double Frequency { get; set; }
        public double Current { get; set; }
        public double Voltage { get; set; }
        public double Phase { get; set; }
        public double Power { get; set; }
        public void CalcPower()
        {
            Power = Voltage * Current;
        }
    }
}
