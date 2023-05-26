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
    internal class PicoData
    {
        public PicoData()
        {
            RawData = new List<WaveformData>();
        }
        public List<WaveformData> RawData { get; set; }
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
            for (int i = 0; i < 50; i++)
            {
                RawData.Add(new WaveformData()
                {
                    Time = i,
                    Voltage = rand.NextDouble(),
                    Current = rand.NextDouble(),
                });
            }
        }
    }
}
