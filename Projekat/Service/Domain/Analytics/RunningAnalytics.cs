using Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Globalization;

namespace Service.Domain.Analytics
{
    public sealed class RunningAnalytics
    {
        public double EnergyKWh { get; private set; }
        public DateTime? LastTs { get; private set; }
        public double? LastFreq { get; private set; }
        public double? LastFreqMin { get; private set; }
        public double? LastFreqMax { get; private set; }
        public double? LastPowerAvg { get; private set; }
        public int StallRun { get; private set; }  
        public double LastDeltaE_KWh { get; private set; }

        readonly double overloadKw;             
        readonly double spikeHz;              
        readonly double devHz;              
        readonly int stallWindow;         
        readonly double stallEpsKWh;                
        readonly double stallPowerDeltaEpsKw;     
        readonly string stallMode;               

        double nominalHz;
        bool nominalFixed;
        readonly bool autoNominal;
        readonly int warmupN = 8;
        readonly Queue<double> warmup = new Queue<double>();

        static double ReadDouble(string key, double def)
        {
            var s = ConfigurationManager.AppSettings[key];
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;
        }
        static int ReadInt(string key, int def)
        {
            var s = ConfigurationManager.AppSettings[key];
            return int.TryParse(s, out var v) ? v : def;
        }

        public RunningAnalytics()
        {
            overloadKw = ReadDouble("OverloadKw", 7.0);
            spikeHz = ReadDouble("FreqSpikeHz", 0.3);
            devHz = ReadDouble("FreqDeviationHz", 0.5);
            stallWindow = ReadInt("StallWindow", 10);
            stallEpsKWh = ReadDouble("StallEpsKWh", 1e-4);
            stallPowerDeltaEpsKw = ReadDouble("StallPowerDeltaEpsKw", 0.05);
            stallMode = ConfigurationManager.AppSettings["StallMode"]?.ToLowerInvariant() ?? "both";

            var nf = ConfigurationManager.AppSettings["NominalFreqHz"];
            if (double.TryParse(nf, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
            {
                nominalHz = f;
                nominalFixed = true;
                autoNominal = false;
            }
            else
            {
                autoNominal = true; 
                nominalFixed = false;
                nominalHz = 0;
            }
        }

        public IEnumerable<string> UpdateAndCheck(SampleDto s)
        {
            var codes = new List<string>();

            double dE = 0.0;
            if (LastTs.HasValue)
            {
                var dt = (s.Timestamp - LastTs.Value).TotalSeconds;
                if (dt > 0) { dE = s.RealPowerAvg * dt / 3600.0; EnergyKWh += dE; }
            }
            LastDeltaE_KWh = dE;

            var dP = LastPowerAvg.HasValue ? Math.Abs(s.RealPowerAvg - LastPowerAvg.Value) : double.MaxValue;

            bool lowEnergy = dE <= stallEpsKWh;
            bool flatPower = dP <= stallPowerDeltaEpsKw;
            bool stallCond =
                stallMode == "energy" ? lowEnergy :
                stallMode == "powerflat" ? flatPower :
                                           (lowEnergy || flatPower);

            if (stallCond) StallRun++;
            else StallRun = 0;

            if (StallRun > stallWindow) codes.Add("ENERGY_STALL");

            if (s.RealPowerMax > overloadKw) codes.Add("OVERLOAD");

            if (!nominalFixed && autoNominal)
            {
                warmup.Enqueue(s.FrequencyAvg);
                if (warmup.Count > warmupN) warmup.Dequeue();
                if (warmup.Count == warmupN) nominalHz = warmup.Average();
            }
            var haveNominal = nominalFixed || warmup.Count > 0;
            if (haveNominal)
            {
                var nom = nominalFixed ? nominalHz : warmup.Average();
                if (Math.Abs(s.FrequencyAvg - nom) > devHz)
                    codes.Add("FREQUENCY_OUT_OF_RANGE");
            }

            if (LastFreq.HasValue)
            {
                var dAvg = Math.Abs(s.FrequencyAvg - LastFreq.Value);
                var dMin = LastFreqMin.HasValue ? Math.Abs(s.FrequencyMin - LastFreqMin.Value) : 0.0;
                var dMax = LastFreqMax.HasValue ? Math.Abs(s.FrequencyMax - LastFreqMax.Value) : 0.0;
                var d = Math.Max(dAvg, Math.Max(dMin, dMax));
                if (d > spikeHz) codes.Add("FREQUENCY_SPIKE");
            }

            LastTs = s.Timestamp;
            LastFreq = s.FrequencyAvg;
            LastFreqMin = s.FrequencyMin;
            LastFreqMax = s.FrequencyMax;
            LastPowerAvg = s.RealPowerAvg;

            return codes;
        }
    }
}
