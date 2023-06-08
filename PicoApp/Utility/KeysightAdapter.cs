using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PicoApp.Model;
using NationalInstruments.Visa;

namespace PicoApp.Utility
{
    internal class KeysightAdapter
    {
        List<string> resources { get; set; }
        public List<OscopeData> ScopeData { get; set; }
        double curFreq = 0D;
        double lastFreq = 0D;
        double voltageRMS = 0D;
        double currentRMS = 0D;
        double phase = 0D;
        double[] rawData = null;
        Task FrequencyMonitor { get; set; }
        private readonly double freqThreshold;
        public KeysightAdapter(double freqThreshold)
        {
            ScopeData = new List<OscopeData>();
            this.freqThreshold = freqThreshold;
            FrequencyMonitor = MonitorFrequency();
        }
        private async Task MonitorFrequency()
        {
            if (curFreq >= lastFreq + freqThreshold || curFreq <= lastFreq - freqThreshold)
            {
                TriggerData();
                lastFreq = curFreq;
            }
            await Task.Delay(100);
        }
        private void TriggerData()
        {
            OscopeData data = new();
            data.CurrentRMS = currentRMS;
            data.VoltageRMS = voltageRMS;
            data.Phase = phase;
            foreach (var v in rawData)
            {
                WaveformData waveformData = new WaveformData();
            }
            ScopeData.Add(data);
        }
    }
}
