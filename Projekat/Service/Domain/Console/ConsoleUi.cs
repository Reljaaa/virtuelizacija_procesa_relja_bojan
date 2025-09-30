using System;
using System.Configuration;
using System.Text;
using Common;
using Service.Domain.Writers;

namespace Service.Domain
{
    public static class ConsoleUi
    {
        public static readonly bool DumpCsv =
            bool.TryParse(ConfigurationManager.AppSettings["ConsoleCsvDump"], out var b) && b;

        private static bool _headerPrinted;

        public static void Init()
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

        public static void SessionStart(int sessionId, string vehicle)
        {
            _headerPrinted = false;
            Info($"[START] Session {sessionId} for Vehicle={vehicle}");
        }

        public static void PrintHeader()
        {
            var line = new string('-', 96);
            Console.WriteLine(line);
            Console.WriteLine(
                $"{"#",4} {"Time",8} {"VrmsAvg",8} {"IrmsAvg",8} {"Pavg(kW)",9} {"Pmax",9} {"fAvg",7} {"fMin",7} {"fMax",7}");
            Console.WriteLine(line);
            _headerPrinted = true;
        }

        public static void Row(int ordinal, SampleDto s)
        {
            if (!_headerPrinted) PrintHeader();
            Console.WriteLine(
                $"{ordinal,4} {s.Timestamp:HH:mm:ss} {s.VoltageRmsAvg,8:F1} {s.CurrentRmsAvg,8:F3} {s.RealPowerAvg,9:F3} {s.RealPowerMax,9:F3} {s.FrequencyAvg,7:F3} {s.FrequencyMin,7:F3} {s.FrequencyMax,7:F3}");
            if (DumpCsv) Console.WriteLine($"[CSV] {SessionWriter.FormatCsvLine(s)}");
        }

        public static void Warn(int sessionId, string code, string message)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARN] #{sessionId} {code}: {message}");
            Console.ForegroundColor = prev;
        }

        public static void Reject(int sessionId, int rowIndex, string field, string msg, SampleDto s)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[REJECT] #{sessionId} Row={rowIndex} {field}: {msg}");
            if (DumpCsv) Console.WriteLine($"[CSV] {SessionWriter.FormatCsvLine(s)}");
            Console.ForegroundColor = prev;
        }

        public static void SessionEnd(int sessionId, int total, int accepted, int rejected)
        {
            Console.WriteLine(new string('-', 96));
            Info($"[END] Session {sessionId}: total={total}, accepted={accepted}, rejected={rejected}");
        }

        public static void Info(string msg) => Console.WriteLine(msg);
    }
}
