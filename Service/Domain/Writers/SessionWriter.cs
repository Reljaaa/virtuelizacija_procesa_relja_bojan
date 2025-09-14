using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Domain.Writers
{
    public class SessionWriter : IDisposable
    {
        public int SessionId { get; }
        public string VehicleId { get; }
        public string DirectoryPath { get; }
        public string SessionCsvPath { get; }
        public string RejectsCsvPath { get; }

        private readonly StreamWriter _sessionWriter;
        private readonly StreamWriter _rejectsWriter;
        private bool _disposed = false;

        public SessionWriter(int sessionId, string vehicleId)
        {
            SessionId = sessionId;
            VehicleId = vehicleId;
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            DirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", VehicleId, date);
            
            Directory.CreateDirectory(DirectoryPath);
            SessionCsvPath = Path.Combine(DirectoryPath, "session.csv");
            RejectsCsvPath = Path.Combine(DirectoryPath, "rejects.csv");
           
            var sfs = new FileStream(SessionCsvPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            _sessionWriter = new StreamWriter(sfs) { AutoFlush = true };
            if (sfs.Length == 0)
            {
                _sessionWriter.WriteLine("Timestamp,VoltMin,VoltAvg,VoltMax,CurrMin,CurrAvg,CurrMax,RealMin,RealAvg,RealMax,ReacMin,ReacAvg,ReacMax,AppMin,AppAvg,AppMax,FreqMin,FreqAvg,FreqMax,RowIndex,VehicleId");
            }

            var rfs = new FileStream(RejectsCsvPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            _rejectsWriter = new StreamWriter(rfs) { AutoFlush = true };
            if (rfs.Length == 0)
            {
                _rejectsWriter.WriteLine("RowIndex,Field,Message");
            }
        }
        public void WriteSample(SampleDto s)
        {
            string line = string.Join(",",
                s.Timestamp.ToString("o", CultureInfo.InvariantCulture),
                s.VoltageRmsMin.ToString(CultureInfo.InvariantCulture),
                s.VoltageRmsAvg.ToString(CultureInfo.InvariantCulture),
                s.VoltageRmsMax.ToString(CultureInfo.InvariantCulture),
                s.CurrentRmsMin.ToString(CultureInfo.InvariantCulture),
                s.CurrentRmsAvg.ToString(CultureInfo.InvariantCulture),
                s.CurrentRmsMax.ToString(CultureInfo.InvariantCulture),
                s.RealPowerMin.ToString(CultureInfo.InvariantCulture),
                s.RealPowerAvg.ToString(CultureInfo.InvariantCulture),
                s.RealPowerMax.ToString(CultureInfo.InvariantCulture),
                s.ReactivePowerMin.ToString(CultureInfo.InvariantCulture),
                s.ReactivePowerAvg.ToString(CultureInfo.InvariantCulture),
                s.ReactivePowerMax.ToString(CultureInfo.InvariantCulture),
                s.ApparentPowerMin.ToString(CultureInfo.InvariantCulture),
                s.ApparentPowerAvg.ToString(CultureInfo.InvariantCulture),
                s.ApparentPowerMax.ToString(CultureInfo.InvariantCulture),
                s.FrequencyMin.ToString(CultureInfo.InvariantCulture),
                s.FrequencyAvg.ToString(CultureInfo.InvariantCulture),
                s.FrequencyMax.ToString(CultureInfo.InvariantCulture),
                s.RowIndex.ToString(CultureInfo.InvariantCulture),
                s.VehicleId
            );
            _sessionWriter.WriteLine(line);
        }
        public void WriteReject(int rowIndex, string field, string message)
        { 
            var safeMsg = (message ?? "").Replace("\"", "''");
            _rejectsWriter.WriteLine($"{rowIndex},{field},\"{safeMsg}\"");
        }
        public void Dispose()
        {
            if (!_disposed)
            {
                _sessionWriter?.Dispose();
                _rejectsWriter?.Dispose();
                _disposed = true;
            }
        }
    }
}
