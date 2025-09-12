using Common;
using Service.Domain.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class ChargingService: IChargingService
    {
        private static int _nextSessionId = 1;
        public StartSessionResponse StartSession(StartSessionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.VehicleId))
            {
                Console.WriteLine("[REJECT] StartSession: VechileId je obavezan!");
                return new StartSessionResponse { SessionId = 0 };
            }
            int id = _nextSessionId++;
            Console.WriteLine($"[START] Session {id} started for vehicle {request.VehicleId}.");
            return new StartSessionResponse { SessionId = id };

        }

        public void PushSample(int sessionId, SampleDto sample)
        {
            var fault = SampleValidator.ValidateSample(sample);
            if(fault != null)
            {
                Console.WriteLine($"[REJECT] Session {sessionId} - Sample rejected: {fault.Message} (Row: {fault.RowIndex}, Field: {fault.FieldName})");
                return;
            }
            Console.WriteLine($"[ACCEPT] Session {sessionId} - Sample accepted: VehicleId={sample.VehicleId}, Timestamp={sample.Timestamp}, FrequencyAvg={sample.FrequencyAvg}, VoltageRmsAvg={sample.VoltageRmsAvg}, CurrentRmsAvg={sample.CurrentRmsAvg}");

        }
        public void EndSession(int sessionId)
        {
            Console.WriteLine($"[END] Session {sessionId} ended.");
        }
    }
}
