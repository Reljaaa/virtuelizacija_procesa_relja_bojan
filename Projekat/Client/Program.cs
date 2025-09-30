using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;

namespace Client
{
    public class Program
    {
        // Indeksi kolona u CSV-u
        const int IDX_TS = 0;  // Date Time
        const int IDX_VRMS_MIN = 1; const int IDX_VRMS_AVG = 2; const int IDX_VRMS_MAX = 3;
        const int IDX_IRMS_MIN = 4; const int IDX_IRMS_AVG = 5; const int IDX_IRMS_MAX = 6;
        const int IDX_RP_MIN = 7; const int IDX_RP_AVG = 8; const int IDX_RP_MAX = 9;
        const int IDX_QP_MIN = 10; const int IDX_QP_AVG = 11; const int IDX_QP_MAX = 12;
        const int IDX_SP_MIN = 13; const int IDX_SP_AVG = 14; const int IDX_SP_MAX = 15;
        const int IDX_F_MIN = 16; const int IDX_F_AVG = 17; const int IDX_F_MAX = 18;

        static void Main(string[] args)
        {
            try
            {
                var (vehicleId, csvPath) = PickVehicleAndCsv();
                Console.WriteLine($"\nSelected vehicle: {vehicleId}");
                Console.WriteLine($"CSV path: {csvPath}");

                if (!File.Exists(csvPath))
                {
                    Console.WriteLine($"[ERROR] CSV ne postoji: {csvPath}");
                    PauseExit();
                    return;
                }

                var rejectsPath = Path.Combine(Path.GetDirectoryName(csvPath) ?? "", "rejected.csv");
                EnsureRejectsHeader(rejectsPath);

                var factory = new ChannelFactory<IChargingService>("ChargingTcp");
                var proxy = factory.CreateChannel();
                var ch = (IClientChannel)proxy;

                int sessionId = 0, rowIndex = 0, accepted = 0, rejected = 0;

                try
                {
                    var start = proxy.StartSession(new StartSessionRequest { VehicleId = vehicleId });
                    if (start.SessionId <= 0)
                    {
                        Console.WriteLine("[ERROR] Session could not be started!");
                        SafeAbort(ch, factory);
                        PauseExit();
                        return;
                    }

                    sessionId = start.SessionId;
                    Console.WriteLine($"\n[START] Session ID: {sessionId}\n");

                    using (var fs = new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                    {
                        string line;
                        bool headerSkipped = false;

                        while ((line = sr.ReadLine()) != null)
                        {
                            if (!headerSkipped)
                            {
                                // preskoči header ako postoji (počinje sa "Date Time")
                                if (line.TrimStart().StartsWith("Date Time", StringComparison.OrdinalIgnoreCase))
                                {
                                    headerSkipped = true;
                                    continue;
                                }
                                headerSkipped = true; // ako nema header-a, prvi red je odmah podatak
                            }

                            rowIndex++;

                            var f = line.Split(',');
                            if (f.Length < 19)
                            {
                                AppendReject(rejectsPath, rowIndex, "Row", $"Premalo kolona: {f.Length} (očekivano 19+)");
                                rejected++;
                                continue;
                            }

                            // Parsiranje obaveznih polja
                            if (!TryTimestamp(f[IDX_TS], out var ts))
                            {
                                AppendReject(rejectsPath, rowIndex, "Timestamp", $"Neispravan datum/vreme: {f[IDX_TS]}");
                                rejected++; continue;
                            }
                            if (!TryDouble(f[IDX_VRMS_AVG], out var vAvg))
                            {
                                AppendReject(rejectsPath, rowIndex, "VoltageRmsAvg", $"Neispravan broj: {f[IDX_VRMS_AVG]}");
                                rejected++; continue;
                            }
                            if (!TryDouble(f[IDX_IRMS_AVG], out var iAvg))
                            {
                                AppendReject(rejectsPath, rowIndex, "CurrentRmsAvg", $"Neispravan broj: {f[IDX_IRMS_AVG]}");
                                rejected++; continue;
                            }
                            if (!TryDouble(f[IDX_RP_AVG], out var pAvg))
                            {
                                AppendReject(rejectsPath, rowIndex, "RealPowerAvg", $"Neispravan broj: {f[IDX_RP_AVG]}");
                                rejected++; continue;
                            }
                            if (!TryDouble(f[IDX_F_AVG], out var fAvg))
                            {
                                AppendReject(rejectsPath, rowIndex, "FrequencyAvg", $"Neispravan broj: {f[IDX_F_AVG]}");
                                rejected++; continue;
                            }

                            // Sastavi DTO
                            var sample = new SampleDto
                            {
                                Timestamp = ts,
                                VoltageRmsAvg = vAvg,
                                CurrentRmsAvg = iAvg,
                                RealPowerAvg = pAvg,
                                FrequencyAvg = fAvg,
                                RowIndex = rowIndex,
                                VehicleId = vehicleId,

                                VoltageRmsMin = ParseOrDefault(f[IDX_VRMS_MIN]),
                                VoltageRmsMax = ParseOrDefault(f[IDX_VRMS_MAX]),
                                CurrentRmsMin = ParseOrDefault(f[IDX_IRMS_MIN]),
                                CurrentRmsMax = ParseOrDefault(f[IDX_IRMS_MAX]),
                                RealPowerMin = ParseOrDefault(f[IDX_RP_MIN]),
                                RealPowerMax = ParseOrDefault(f[IDX_RP_MAX]),
                                ReactivePowerMin = ParseOrDefault(f[IDX_QP_MIN]),
                                ReactivePowerAvg = ParseOrDefault(f[IDX_QP_AVG]),
                                ReactivePowerMax = ParseOrDefault(f[IDX_QP_MAX]),
                                ApparentPowerMin = ParseOrDefault(f[IDX_SP_MIN]),
                                ApparentPowerAvg = ParseOrDefault(f[IDX_SP_AVG]),
                                ApparentPowerMax = ParseOrDefault(f[IDX_SP_MAX]),
                                FrequencyMin = ParseOrDefault(f[IDX_F_MIN]),
                                FrequencyMax = ParseOrDefault(f[IDX_F_MAX]),
                            };

                            try
                            {
                                proxy.PushSample(sessionId, sample);
                                accepted++;
                                Thread.Sleep(50); // mali tempo da se lepo vidi tok
                            }
                            catch (FaultException<ValidationFault> fx)
                            {
                                var det = fx.Detail;
                                AppendReject(rejectsPath, rowIndex, det.FieldName, det.Message);
                                rejected++;
                            }
                        }
                    }

                    // normalan kraj
                    proxy.EndSession(sessionId);
                    SafeClose(ch, factory);
                    Console.WriteLine($"\n[END] Session ended. Total: {rowIndex}, accepted: {accepted}, rejected: {rejected}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ERROR] " + ex.Message);
                    SafeAbort(ch, factory);
                }
            }
            catch (Exception exTop)
            {
                Console.WriteLine("[FATAL] " + exTop);
            }

            PauseExit();
        }

        // ————— Pomocne funkcije —————

        static (string vehicleId, string csvPath) PickVehicleAndCsv()
        {
            var dataRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            Directory.CreateDirectory(dataRoot);

            var vehicles = Directory.EnumerateDirectories(dataRoot)
                                    .Select(Path.GetFileName)
                                    .OrderBy(n => n)
                                    .ToList();

            Console.WriteLine("Available vehicles:");
            for (int i = 0; i < vehicles.Count; i++)
                Console.WriteLine($"{i + 1}. {vehicles[i]}");

            Console.Write($"Select vehicle (1..{vehicles.Count}): ");
            int vehicleIndex;
            while (!int.TryParse(Console.ReadLine(), out vehicleIndex) ||
                   vehicleIndex < 1 || vehicleIndex > vehicles.Count)
            {
                Console.Write($"[1..{vehicles.Count}]: ");
            }

            string vehicleId = vehicles[vehicleIndex - 1];
            string csvPath = Path.Combine(dataRoot, vehicleId, "Charging_Profile.csv");
            return (vehicleId, csvPath);
        }

        static bool TryTimestamp(string s, out DateTime dt)
        {
            // prvo pokušaj standardni format iz CSV-a "yyyy-MM-dd HH:mm:ss"
            if (DateTime.TryParseExact(s.Trim(), "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return true;

            // fallback na generalni parser (ako dođe drugačiji format)
            return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
        }

        static bool TryDouble(string s, out double d)
        {
            return double.TryParse((s ?? "").Replace(',', '.'),
                NumberStyles.Float, CultureInfo.InvariantCulture, out d);
        }

        static double ParseOrDefault(string s, double def = 0)
        {
            return double.TryParse((s ?? "").Replace(',', '.'),
                NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : def;
        }

        static void EnsureRejectsHeader(string path)
        {
            try
            {
                if (!File.Exists(path))
                    File.WriteAllText(path, "RowIndex,Field,Message\n");
            }
            catch { /* best-effort */ }
        }

        static void AppendReject(string path, int rowIndex, string field, string message)
        {
            try
            {
                File.AppendAllText(path, $"{rowIndex},{field},\"{message.Replace("\"", "''")}\"{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARN] Ne mogu da upišem rejected.csv: " + ex.Message);
            }
        }

        static void SafeClose(IClientChannel ch, ChannelFactory<IChargingService> factory)
        {
            try { if (ch.State != CommunicationState.Closed) ch.Close(); } catch { ch.Abort(); }
            try { factory.Close(); } catch { factory.Abort(); }
        }

        static void SafeAbort(IClientChannel ch, ChannelFactory<IChargingService> factory)
        {
            try { ch.Abort(); } catch { }
            try { factory.Abort(); } catch { }
        }

        static void PauseExit()
        {
            Console.WriteLine("\nPress ENTER to exit...");
            Console.ReadLine();
        }
    }
}
