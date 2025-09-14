using Common;
using Service.Domain.Validation;
using Service.Domain.Writers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class ChargingService : IChargingService
    {
        private static int _nextSessionId = 1;

        private static readonly Dictionary<int, SessionWriter> _sessions = new Dictionary<int, SessionWriter>();

        private static readonly Dictionary<int, int> _rowReceived = new Dictionary<int, int>();
        public StartSessionResponse StartSession(StartSessionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.VehicleId))
            {
                Console.WriteLine("[REJECT] StartSession: VechileId je obavezan!");
                return new StartSessionResponse { SessionId = 0 };
            }
            int id = _nextSessionId++;

            var writer = new SessionWriter(id, request.VehicleId);

            _sessions[id] = writer;
            _rowReceived[id] = 0;

            Console.WriteLine($"[START] Session {id} started for vehicle {request.VehicleId}.");
            return new StartSessionResponse { SessionId = id };

        }

        public void PushSample(int sessionId, SampleDto sample)
        {
            if (!_sessions.TryGetValue(sessionId, out var writer))
            {
                Console.WriteLine($"[WARN] PushSample for unknown SessionId={sessionId}");
                return;
            }

            var fault = SampleValidator.ValidateSample(sample);
            if (fault != null)
            {
                writer.WriteReject(sample.RowIndex, fault.FieldName, fault.Message);
                throw new FaultException<ValidationFault>(fault, new FaultReason(fault.Message));
            }

            if (_rowReceived.TryGetValue(sessionId, out var count) && count == 0)
            {
                Console.WriteLine($"[STATUS] Session {sessionId}: prenos u toku...");
            }
            _rowReceived[sessionId] = count + 1;
            writer.WriteSample(sample);
            Console.WriteLine($"\n[SAMPLE][{_rowReceived[sessionId]}] VehicleId={sample.VehicleId}, Timestamp={sample.Timestamp}, FrequencyAvg={sample.FrequencyAvg}, VoltageRmsAvg={sample.VoltageRmsAvg}, CurrentRmsAvg={sample.CurrentRmsAvg}");
        }
        public void EndSession(int sessionId)
        {
            if (_sessions.TryGetValue(sessionId, out var writer))
            {
                writer.Dispose();
                _sessions.Remove(sessionId);
            }
            if (_rowReceived.TryGetValue(sessionId, out var count))
            {
                _rowReceived.Remove(sessionId);
                Console.WriteLine($"\n[END] Session {sessionId}: prenos zavrsen...");
                Console.WriteLine($"[END] Ukupno {count} zapisa ");
            }
        }
    }
}
