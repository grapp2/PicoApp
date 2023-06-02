using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicoApp.Model
{
    internal class DhmData
    {
        public double Frequency { get; set; }
        public double Displacement { get; set; }
    }

    internal class GroupDhmData
    {
        public GroupDhmData()
        {
            RawData = new List<DhmData>();
            GroupedData = new List<DhmData>();
        }
        public List<DhmData> RawData { get; set; }
        public List<DhmData> GroupedData { get; set;}

        public void GroupData()
        {
            GroupedData = new List<DhmData>();
            double min = double.MaxValue;
            double max = 0;
            double lastFreq = RawData[0].Frequency;
            foreach (var data in RawData)
            {
                if (data.Frequency != lastFreq)
                {
                    double displacement = max - min;
                    var GroupDisplacement = new DhmData();
                    GroupDisplacement.Frequency = lastFreq;
                    GroupDisplacement.Displacement = displacement;
                    GroupedData.Add(GroupDisplacement);
                    lastFreq = data.Frequency;
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
        }
    }
}
