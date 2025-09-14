using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class Program
    {
        // —— KONSTANTE ZA INDEKSE (prema tvom CSV) ——
        const int IDX_TS = 0;   // Date Time
        const int IDX_VRMS_MIN = 1;   // Voltage RMS Min (V)
        const int IDX_VRMS_AVG = 2;   // Voltage RMS Avg (V)
        const int IDX_VRMS_MAX = 3;   // Voltage RMS Max (V)
        const int IDX_IRMS_MIN = 4;   // Current RMS Min (A)
        const int IDX_IRMS_AVG = 5;   // Current RMS Avg (A)
        const int IDX_IRMS_MAX = 6;   // Current RMS Max (A)
        const int IDX_RP_MIN = 7;   // Real Power Min (kW)
        const int IDX_RP_AVG = 8;   // Real Power Avg (kW)
        const int IDX_RP_MAX = 9;   // Real Power Max (kW)
        const int IDX_QP_MIN = 10;  // Reactive Power Min (kVAR)
        const int IDX_QP_AVG = 11;  // Reactive Power Avg (kVAR)
        const int IDX_QP_MAX = 12;  // Reactive Power Max (kVAR)
        const int IDX_SP_MIN = 13;  // Apparent Power Min (kVA)
        const int IDX_SP_AVG = 14;  // Apparent Power Avg (kVA)
        const int IDX_SP_MAX = 15;  // Apparent Power Max (kVA)
        const int IDX_F_MIN = 16;  // Frequency Min (Hz)
        const int IDX_F_AVG = 17;  // Frequency Avg (Hz)
        const int IDX_F_MAX = 18;  // Frequency Max (Hz)

        static void Main(string[] args)
        {
            var (vechileId, csvPath) = PickVehicleAndCsv();
            Console.WriteLine($"\nSelected vehicle: {vechileId}");
            Console.WriteLine($"CSV path: {csvPath}");

            ChannelFactory<IChargingService> factory = new ChannelFactory<IChargingService>("ChargingTcp");
            IChargingService proxy = factory.CreateChannel();
            try
            {
                var start = proxy.StartSession(new StartSessionRequest { VehicleId = vechileId });
                if (start.SessionId <= 0)
                {
                    Console.WriteLine("\n[ERROR] Session could not be started!");
                    return;
                }
                Console.WriteLine($"\n[START] Session started with ID: {start.SessionId}");

                var rejectsPath = Path.Combine(Path.GetDirectoryName(csvPath) ?? string.Empty, "rejected.csv");

                int rowIndex = 0, accepted = 0, rejected = 0;

                using (var sr = new StreamReader(csvPath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (rowIndex == 0 && line.TrimStart().StartsWith("Date Time", StringComparison.OrdinalIgnoreCase))
                            continue;

                        rowIndex++;

                        var f = line.Split(',');

                        if (!TryTimestamp(f[IDX_TS], out var ts))
                        {
                            File.AppendAllText(rejectsPath, $"{rowIndex},Timestamp,Neispravan datum i vrijeme: {f[IDX_TS]}\n");
                            rejected++; continue;
                        }
                        if (!TryDouble(f[IDX_VRMS_AVG], out var vAvg))
                        {
                            File.AppendAllText(rejectsPath, $"{rowIndex},VoltageRmsAvg,Neispravan broj: {f[IDX_VRMS_AVG]}\n");
                            rejected++; continue;
                        }
                        if (!TryDouble(f[IDX_IRMS_AVG], out var iAvg))
                        {
                            File.AppendAllText(rejectsPath, $"{rowIndex},CurrentRmsAvg,Neispravan broj: {f[IDX_IRMS_AVG]}\n");
                            rejected++; continue;
                        }
                        if (!TryDouble(f[IDX_RP_AVG], out var pAvg))
                        {
                            File.AppendAllText(rejectsPath, $"{rowIndex},RealPowerAvg,Neispravan broj: {f[IDX_RP_AVG]}\n");
                            rejected++; continue;
                        }
                        if (!TryDouble(f[IDX_F_AVG], out var fAvg))
                        {
                            File.AppendAllText(rejectsPath, $"{rowIndex},FrequencyAvg,Neispravan broj: {f[IDX_F_AVG]}\n");
                            rejected++; continue;
                        }

                        var sample = new SampleDto
                        {
                            Timestamp = ts,
                            VoltageRmsAvg = vAvg,
                            CurrentRmsAvg = iAvg,
                            RealPowerAvg = pAvg,
                            FrequencyAvg = fAvg,
                            RowIndex = rowIndex,
                            VehicleId = vechileId,

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
                            proxy.PushSample(start.SessionId, sample);
                            accepted++;
                            System.Threading.Thread.Sleep(50); 
                        }
                        catch (FaultException<ValidationFault> fx)
                        {
                            var det = fx.Detail;
                            File.AppendAllText(rejectsPath, $"{rowIndex},{det.FieldName},{det.Message}\n");
                            rejected++;
                        }

                    }
                }
                proxy.EndSession(start.SessionId);
                Console.WriteLine($"\n[END] Session ended. Total samples: {rowIndex}, accepted: {accepted}, rejected: {rejected} ({rejectsPath}).");
                factory.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR]{ex.Message}");
            }
            Console.WriteLine("\n\nPress ENTER to exit...");
            Console.ReadLine();
        }

        static (string vechileId, string csvPath) PickVehicleAndCsv()
        {
            var dataRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            var vehicles = Directory.GetDirectories(dataRoot)
                                    .Select(d => new DirectoryInfo(d).Name)
                                    .OrderBy(n => n)
                                    .ToList();

            Console.WriteLine("Available vehicles:");
            for (int i = 0; i < vehicles.Count; i++)
                Console.WriteLine($"{i + 1}. {vehicles[i]}");

            Console.Write("Select vehicle (number): ");
            int vehicleIndex;
            while (!int.TryParse(Console.ReadLine(), out vehicleIndex) || vehicleIndex < 1 || vehicleIndex > vehicles.Count)
                Console.Write($"[1..{vehicles.Count}]: ");

            string vechileId = vehicles[vehicleIndex - 1];
            string csvPath = Path.Combine(dataRoot, vechileId, "Charging_Profile.csv");
            return (vechileId, csvPath);
        }

        static bool TryTimestamp(string s, out DateTime dt)
        {
            return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
        }

        static bool TryDouble(string s, out double d)
        {
            return double.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out d);
        }

        static double ParseOrDefault(string s, double def = 0)
        {
            // najjednostavnije: zamena zareza tačkom + InvariantCulture
            return double.TryParse(s.Replace(',', '.'), NumberStyles.Float,
                                   CultureInfo.InvariantCulture, out var d) ? d : def;
        }


    }
}
