using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicoApp.Model
{
    internal class WaveformData
    {
        public double Time { get; set; }
        public double Current { get; set; }
        public double Voltage { get; set; }
    }
    internal class OscopeData
    {
        public OscopeData()
        {
            RawData = new List<double>();
        }
        public List<double> RawData { get; set; }
        public double CurrentRMS { get; set; }
        public double VoltageRMS { get; set; }
        public double Phase { get; set; }
        public double Power { get; set; }
        public void CalcPower()
        {
            Power = VoltageRMS * CurrentRMS * Math.Cos(Phase);
        }
        public void GenerateSample()
        {
            Random rand = new Random();
            CurrentRMS = rand.NextDouble();
            VoltageRMS = rand.NextDouble();
            Phase = rand.NextDouble();
            Power = rand.NextDouble();
        }
    }
}
