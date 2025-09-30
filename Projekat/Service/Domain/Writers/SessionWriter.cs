using Common;
using System;
using System.Globalization;
using System.IO;

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
        private bool _disposed;

        // Jedinstveni header (isti redosled kao u FormatCsvLine)
        public static readonly string CsvHeader =
            "Timestamp,VoltageRmsMin,VoltageRmsAvg,VoltageRmsMax," +
            "CurrentRmsMin,CurrentRmsAvg,CurrentRmsMax," +
            "RealPowerMin,RealPowerAvg,RealPowerMax," +
            "ReactivePowerMin,ReactivePowerAvg,ReactivePowerMax," +
            "ApparentPowerMin,ApparentPowerAvg,ApparentPowerMax," +
            "FrequencyMin,FrequencyAvg,FrequencyMax,RowIndex,VehicleId";

        // Formatter za jedan CSV red (koristi ga i fajl i konzola)
        public static string FormatCsvLine(SampleDto s)
        {
            return string.Join(",",
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
        }

        public SessionWriter(int sessionId, string vehicleId, string baseDir)
        {
            SessionId = sessionId;
            VehicleId = vehicleId ?? string.Empty;

            var datePart = DateTime.Now.ToString("yyyy-MM-dd");
            DirectoryPath = Path.Combine(baseDir ?? "", VehicleId, datePart);
            Directory.CreateDirectory(DirectoryPath);

            SessionCsvPath = Path.Combine(DirectoryPath, "session.csv");
            RejectsCsvPath = Path.Combine(DirectoryPath, "rejects.csv");

            var sfs = new FileStream(SessionCsvPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            _sessionWriter = new StreamWriter(sfs) { AutoFlush = true };
            if (sfs.Length == 0) _sessionWriter.WriteLine(CsvHeader);

            var rfs = new FileStream(RejectsCsvPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            _rejectsWriter = new StreamWriter(rfs) { AutoFlush = true };
            if (rfs.Length == 0) _rejectsWriter.WriteLine("RowIndex,Field,Message");
        }

        public void WriteSample(SampleDto s)
        {
            _sessionWriter.WriteLine(FormatCsvLine(s));
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


