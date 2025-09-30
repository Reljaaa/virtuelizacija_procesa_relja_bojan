using Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Service.Domain.Analytics
{
    public sealed class RunningAnalytics
    {
        public double EnergyKWh { get; private set; }
        public DateTime? LastTs { get; private set; }

        // za poruke
        public double? LastFreq { get; private set; }
        public double? LastFreqMin { get; private set; }
        public double? LastFreqMax { get; private set; }
        public int LowPowerRun { get; private set; } // sada broji uzastopne ΔE ≈ 0

        readonly double overloadKw;     // prag za RealPowerMax
        readonly double spikeHz;        // prag za spike
        readonly int stallWindow;    // koliko ΔE≈0 redova zaredom
        readonly double stallEpsKWh;    // koliko je "≈0" kWh
        readonly double devHz;          // dozvoljeno odstupanje Avg od nominalne

        double nominalHz;               // ako je definisano brojem u configu
        bool nominalFixed;
        readonly bool autoNominal;
        readonly int warmupN = 8;       // koliko uzoraka za auto-nominal
        readonly Queue<double> warmup = new Queue<double>();

        static double ReadDouble(string key, double def)
        {
            var s = ConfigurationManager.AppSettings[key];
            return double.TryParse(s, System.Globalization.NumberStyles.Float,
                   System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : def;
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
            stallWindow = ReadInt("StallWindow", 10);
            stallEpsKWh = ReadDouble("StallEpsKWh", 1e-4);
            devHz = ReadDouble("FreqDeviationHz", 0.5);

            var nf = ConfigurationManager.AppSettings["NominalFreqHz"];
            if (double.TryParse(nf, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out var f))
            {
                nominalHz = f;
                nominalFixed = true;
                autoNominal = false;
            }
            else
            {
                autoNominal = true;    // "auto" ili nema ključa
                nominalFixed = false;
                nominalHz = 0;
            }
        }

        public IEnumerable<string> UpdateAndCheck(SampleDto s)
        {
            var codes = new List<string>();

            // 1) Energija i "stall"
            double dE = 0.0;
            if (LastTs.HasValue)
            {
                var dt = (s.Timestamp - LastTs.Value).TotalSeconds;
                if (dt > 0)
                {
                    dE = s.RealPowerAvg * dt / 3600.0;
                    EnergyKWh += dE;
                }
            }
            if (dE <= stallEpsKWh) LowPowerRun++; else LowPowerRun = 0;
            if (LowPowerRun > stallWindow) codes.Add("ENERGY_STALL");

            // 2) Overload po RealPowerMax (kW)
            if (s.RealPowerMax > overloadKw) codes.Add("OVERLOAD");

            // 3) Nominalna frekvencija: fiksna ili auto (rolni prosek prvih ~8 uzoraka)
            if (!nominalFixed && autoNominal)
            {
                warmup.Enqueue(s.FrequencyAvg);
                if (warmup.Count > warmupN) warmup.Dequeue();
                if (warmup.Count == warmupN) nominalHz = warmup.Average(); // postavi nominal
            }
            var haveNominal = nominalFixed || warmup.Count > 0;
            if (haveNominal)
            {
                var nom = nominalFixed ? nominalHz : warmup.Average();
                if (Math.Abs(s.FrequencyAvg - nom) > devHz)
                    codes.Add("FREQUENCY_OUT_OF_RANGE");
            }

            // 4) Spike: maksimalna promena između redova po Avg/Min/Max
            if (LastFreq.HasValue)
            {
                var dAvg = Math.Abs(s.FrequencyAvg - LastFreq.Value);
                var dMin = LastFreqMin.HasValue ? Math.Abs(s.FrequencyMin - LastFreqMin.Value) : 0.0;
                var dMax = LastFreqMax.HasValue ? Math.Abs(s.FrequencyMax - LastFreqMax.Value) : 0.0;
                var d = Math.Max(dAvg, Math.Max(dMin, dMax));
                if (d > spikeHz) codes.Add("FREQUENCY_SPIKE");
            }

            // state update
            LastTs = s.Timestamp;
            LastFreq = s.FrequencyAvg;
            LastFreqMin = s.FrequencyMin;
            LastFreqMax = s.FrequencyMax;

            return codes;
        }
    }
}
