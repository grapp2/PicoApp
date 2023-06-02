using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PicoPinnedArray;
using PS2000Imports;

namespace PicoApp.Utility
{
    struct ChannelSettings
    {
        public short DCcoupled;
        public Imports.Range range;
        public short enabled;
    }

    class Pwq
    {
        public Imports.PwqConditions[] conditions;
        public short nConditions;
        public Imports.ThresholdDirection direction;
        public uint lower;
        public uint upper;
        public Imports.PulseWidthType type;

        public Pwq(Imports.PwqConditions[] conditions,
            short nConditions,
            Imports.ThresholdDirection direction,
            uint lower, uint upper,
            Imports.PulseWidthType type)
        {
            this.conditions = conditions;
            this.nConditions = nConditions;
            this.direction = direction;
            this.lower = lower;
            this.upper = upper;
            this.type = type;
        }
    }
    internal class PicoAdapter
    {
        private readonly short _handle;
        public const int BUFFER_SIZE = 1024;
        public const int SINGLE_SCOPE = 1;
        public const int DUAL_SCOPE = 2;
        public const int MAX_CHANNELS = 4;
        public const int COMPATIBLE_STREAMING_MAX_SAMPLES = 60000;

        short _timebase = 8;
        short _oversample = 1;
        bool _hasFastStreaming = false;

        uint _totalSampleCount = 0;
        uint _nValues = 0;
        bool _autoStop;
        short _trig;
        uint _trigAt;
        bool _appBufferFull;
        public short[][] _appBuffer = new short[DUAL_SCOPE][];
        private uint _OverViewBufferSize = 150000;
        private uint _MaxSamples = 1000000;

        ushort[] inputRanges = { 10, 20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000, 50000 };
        private ChannelSettings[] _channelSettings;
        private int _channelCount = DUAL_SCOPE;
        private Imports.Range _firstRange;
        private Imports.Range _lastRange;
        PinnedArray<short>[] pinned = new PinnedArray<short>[4];
        private string BlockFile = "block.txt";
        private string StreamFile = "stream.txt";
        /****************************************************************************
        * CollectBlockImmediate
        *  this function demonstrates how to collect a single block of data
        *  from the unit (start collecting immediately)
        ****************************************************************************/
        void CollectBlockImmediate()
        {
            Console.WriteLine("Collect Block Immediate");
            Console.WriteLine("Data is written to disk file ({0})", BlockFile);
            Console.WriteLine("Press a key to start...");
            Console.WriteLine();

            /* Trigger disabled	*/
            SetTrigger(null, 0, null, 0, null, null, 0, 0);

            BlockDataHandler("First 10 readings", 0);
        }
        /****************************************************************************
         * BlockDataHandler
         * - Used by all block data routines
         * - acquires data (user sets trigger mode before calling), displays 10 items
         *   and saves all to block.txt
         * Input :
         * - text : the text to display before the display of data slice
         * - offset : the offset into the data buffer to start the display's slice.
         ****************************************************************************/
        void BlockDataHandler(string text, int offset)
        {
            int sampleCount = BUFFER_SIZE;
            short timeUnit = 0;
            int timeIndisposed;
            short status = 0;

            // Buffer to hold time data

            int[] times = new int[sampleCount];
            PinnedArray<int> pinnedTimes = new PinnedArray<int>(times);

            // Channel buffers
            for (int i = 0; i < _channelCount; i++)
            {
                short[] buffer = new short[sampleCount];
                pinned[i] = new PinnedArray<short>(buffer);
            }

            /* Find the maximum number of samples, the time interval (in nanoseconds),
                * the most suitable time units (ReportedTimeUnits), and the maximum _oversample at the current _timebase*/
            int timeInterval = 0;
            int maxSamples;

            do
            {
                status = Imports.GetTimebase(_handle, _timebase, sampleCount, out timeInterval, out timeUnit, _oversample, out maxSamples);

                if (status != 1)
                {
                    Console.WriteLine("Selected timebase {0} could not be used\n", _timebase);
                    _timebase++;
                }

            }
            while (status == 0);

            Console.WriteLine("Timebase: {0}\toversample:{1}\n", _timebase, _oversample);

            /* Start the device collecting, then wait for completion*/

            Imports.RunBlock(_handle, sampleCount, _timebase, _oversample, out timeIndisposed);

            Console.WriteLine("Waiting for data...Press a key to abort");
            short ready = 0;

            while (ready == 0 && !Console.KeyAvailable)
            {
                ready = Imports.Isready(_handle);
                Thread.Sleep(1);
            }

            if (Console.KeyAvailable)
            {
                Console.ReadKey(true); // Clear the key
            }

            if (ready > 0)
            {

                short overflow;

                Imports.GetTimesAndValues(_handle, pinnedTimes, pinned[0], pinned[1], null, null, out overflow, timeUnit, sampleCount);

                /* Print out the first 10 readings, converting the readings to mV if required */
                Console.WriteLine(text + "\n");

                for (int ch = 0; ch < _channelCount; ch++)
                {
                    if (_channelSettings[ch].enabled == 1)
                    {
                        Console.Write("Channel {0}\t", (char)('A' + ch));
                    }
                }

                Console.WriteLine("\n");

                for (int i = offset; i < offset + 10; i++)
                {
                    for (int ch = 0; ch < _channelCount; ch++)
                    {
                        if (_channelSettings[ch].enabled == 1)
                        {
                            Console.Write("{0,8}\t", adc_to_mv(pinned[ch].Target[i], (int)_channelSettings[ch].range));
                        }

                    }
                    Console.WriteLine();
                }

                PrintBlockFile(Math.Min(sampleCount, BUFFER_SIZE), pinnedTimes);
            }
            else
            {
                Console.WriteLine("Data collection aborted");
            }

            Imports.Stop(_handle);

        }
        /****************************************************************************
        *  SetTrigger
        *  this function sets all the required trigger parameters, and calls the 
        *  triggering functions
        ****************************************************************************/
        short SetTrigger(Imports.TriggerChannelProperties[] channelProperties,
                        short nChannelProperties,
                        Imports.TriggerConditions[] triggerConditions,
                        short nTriggerConditions,
                        Imports.ThresholdDirection[] directions,
                        Pwq pwq,
                        uint delay,
                        int autoTriggerMs)
        {
            short status = 0;

            status = Imports.SetTriggerChannelProperties(_handle, channelProperties, nChannelProperties, autoTriggerMs);

            if (status == 0)
            {
                return status;
            }

            status = Imports.SetTriggerChannelConditions(_handle, triggerConditions, nTriggerConditions);

            if (status == 0)
            {
                return status;
            }

            if (directions == null)
            {
                directions = new Imports.ThresholdDirection[] { Imports.ThresholdDirection.None,
                                    Imports.ThresholdDirection.None, Imports.ThresholdDirection.None, Imports.ThresholdDirection.None,
                                    Imports.ThresholdDirection.None, Imports.ThresholdDirection.None};

            }


            status = Imports.SetTriggerChannelDirections(_handle,
                                                              directions[(int)Imports.Channel.ChannelA],
                                                              directions[(int)Imports.Channel.ChannelB],
                                                              directions[(int)Imports.Channel.ChannelC],
                                                              directions[(int)Imports.Channel.ChannelD],
                                                              directions[(int)Imports.Channel.External]);

            if (status == 0)
            {
                return status;
            }

            status = Imports.SetTriggerDelay(_handle, delay, 0);

            if (status == 0)
            {
                return status;
            }

            if (pwq == null)
            {
                pwq = new Pwq(null, 0, Imports.ThresholdDirection.None, 0, 0, Imports.PulseWidthType.None);
            }

            status = Imports.SetPulseWidthQualifier(_handle, pwq.conditions,
                                                    pwq.nConditions, pwq.direction,
                                                    pwq.lower, pwq.upper, pwq.type);


            return status;
        }
        /****************************************************************************
         * adc_to_mv
         *
         * Convert an 16-bit ADC count into millivolts
         ****************************************************************************/
        int adc_to_mv(int raw, int ch)
        {
            return (raw * inputRanges[ch]) / Imports.PS2000_MAX_VALUE;
        }
        /// <summary>
        /// Print the block data capture to file
        /// </summary>
        private void PrintBlockFile(int sampleCount, PinnedArray<int> pinnedTimes)
        {
            var sb = new StringBuilder();

            sb.AppendLine("This file contains the following data from a block mode capture:");
            sb.AppendLine("Time interval");
            sb.AppendLine("ADC Count & millivolt (mV) values for each enabled channel.");
            sb.AppendLine();

            // Build Header
            string[] heading = { "Time", "Ch", "ADC Count", "mV" };
            sb.AppendFormat("{0,-10}", heading[0]);

            for (int i = 0; i < _channelCount; i++)
            {
                if (_channelSettings[i].enabled == 1)
                {
                    sb.AppendFormat("{0,-10} {1,-10} {2,-10}", heading[1], heading[2], heading[3]);
                }
            }

            sb.AppendLine();

            // Build Body
            for (int i = 0; i < sampleCount; i++)
            {
                sb.AppendFormat("{0,-10}", pinnedTimes.Target[i]);

                for (int ch = 0; ch < _channelCount; ch++)
                {
                    if (_channelSettings[ch].enabled == 1)
                    {
                        sb.AppendFormat("{0,-10} {1,-10} {2,-10}",
                                        "Ch" + (char)('A' + ch),
                                        pinned[ch].Target[i],
                                        adc_to_mv(pinned[ch].Target[i], (int)_channelSettings[ch].range));
                    }
                }

                sb.AppendLine();
            }

            // Print contents to file
            using (TextWriter writer = new StreamWriter(BlockFile, false))
            {
                writer.Write(sb.ToString());
                writer.Close();
            }
        }
    }
}
