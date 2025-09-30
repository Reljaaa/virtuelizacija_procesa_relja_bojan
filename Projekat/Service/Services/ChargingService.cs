using Common;
using Service.Domain.Validation;
using Service.Domain.Writers;
using Service.Domain.Events;
using Service.Domain.Analytics;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using Service.Domain;
using System.Linq;

namespace Service.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ChargingService : IChargingService
    {
        private static int _nextSessionId = 0;
        private readonly Dictionary<int, SessionWriter> _sessions = new Dictionary<int, SessionWriter>();
        private readonly Dictionary<int, int> _accepted = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _rejected = new Dictionary<int, int>();
        private readonly Dictionary<int, RunningAnalytics> _analytics = new Dictionary<int, RunningAnalytics>();
        private readonly Dictionary<int, string> _vehicleBySession = new Dictionary<int, string>();
        private readonly string _dataRoot;
        private readonly object _gate = new object();
        private bool _disposed;

        public ChargingService()
        {
            _dataRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            Directory.CreateDirectory(_dataRoot);
        }

        public StartSessionResponse StartSession(StartSessionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.VehicleId))
                return new StartSessionResponse { SessionId = 0 };

            var id = System.Threading.Interlocked.Increment(ref _nextSessionId);

            var writer = new SessionWriter(id, request.VehicleId, _dataRoot);
            lock (_gate)
            {
                _sessions[id] = writer;
                _accepted[id] = 0;
                _rejected[id] = 0;
                _analytics[id] = new RunningAnalytics();
                _vehicleBySession[id] = request.VehicleId;
            }

            // Konekcija klijenta – cleanup i ako klijent samo zatvori prozor
            var ch = OperationContext.Current.Channel;
            ch.Closed += (_, __) => SafeEndSession(id, "client channel closed");
            ch.Faulted += (_, __) => SafeEndSession(id, "client channel faulted");

            ConsoleUi.SessionStart(id, request.VehicleId);
            ChargingEventHub.Raise(this, new TransferStartedEvent
            {
                SessionId = id,
                VehicleId = request.VehicleId,
                Timestamp = DateTime.UtcNow
            });

            return new StartSessionResponse { SessionId = id };
        }

        public void PushSample(int sessionId, SampleDto sample)
        {
            if (!_sessions.TryGetValue(sessionId, out var writer))
            {
                ConsoleUi.Warn(sessionId, "UNKNOWN_SESSION", $"PushSample for unknown SessionId={sessionId}");
                return;
            }

            var fault = SampleValidator.ValidateSample(sample);
            if (fault != null)
            {
                writer.WriteReject(sample.RowIndex, fault.FieldName, fault.Message);
                _rejected[sessionId] = _rejected[sessionId] + 1;

                ChargingEventHub.Raise(this, new SampleRejectedEvent
                {
                    SessionId = sessionId,
                    RowIndex = sample.RowIndex,
                    VehicleId = sample.VehicleId,
                    Field = fault.FieldName,
                    Message = fault.Message
                });

                ConsoleUi.Reject(sessionId, sample.RowIndex, fault.FieldName, fault.Message, sample);
                throw new FaultException<ValidationFault>(fault, new FaultReason(fault.Message));
            }

            writer.WriteSample(sample);
            _accepted[sessionId] = _accepted[sessionId] + 1;

            // Lep, poravnat prikaz reda (i opcioni CSV dump ako je ConsoleCsvDump=true)
            var ordinal = _accepted[sessionId] + _rejected[sessionId];
            ConsoleUi.Row(ordinal, sample);

            // Analitika + upozorenja
            var ra = _analytics[sessionId];
            var prevF = ra.LastFreq; // za Δf poruku
            var codes = ra.UpdateAndCheck(sample);

            foreach (var code in codes)
            {
                string msg =
                    code == "OVERLOAD" ? $"RealPowerMax={sample.RealPowerMax:F3} kW" :
                    code == "ENERGY_STALL" ? $"LowPowerRun={ra.LowPowerRun}" :
                    code == "FREQUENCY_OUT_OF_RANGE" ? $"f={sample.FrequencyAvg:F3} Hz" :
                    code == "FREQUENCY_SPIKE" ? (prevF.HasValue ? $"Δf={(Math.Abs(sample.FrequencyAvg - prevF.Value)):F3} Hz" : "") :
                                                        "";

                ChargingEventHub.Raise(this, new WarningEvent
                {
                    SessionId = sessionId,
                    VehicleId = sample.VehicleId,
                    Code = code,
                    Message = msg,
                    Timestamp = DateTime.UtcNow
                });

                ConsoleUi.Warn(sessionId, code, msg);
            }

            ChargingEventHub.Raise(this, new SampleAcceptedEvent
            {
                SessionId = sessionId,
                RowIndex = sample.RowIndex,
                VehicleId = sample.VehicleId
            });
        }


        public void EndSession(int sessionId)
        {
            SafeEndSession(sessionId, "normal end");
        }

        private void SafeEndSession(int sessionId, string reason)
        {
            int total = 0, acc = 0, rej = 0;
            string veh = "";

            lock (_gate)
            {
                if (!_sessions.TryGetValue(sessionId, out var writer))
                    return; // već je očišćeno

                writer.Dispose();
                _sessions.Remove(sessionId);

                if (_accepted.TryGetValue(sessionId, out var a)) { acc = a; _accepted.Remove(sessionId); }
                if (_rejected.TryGetValue(sessionId, out var r)) { rej = r; _rejected.Remove(sessionId); }
                total = acc + rej;

                _analytics.Remove(sessionId);
                if (_vehicleBySession.TryGetValue(sessionId, out var v)) { veh = v; _vehicleBySession.Remove(sessionId); }
            }

            ChargingEventHub.Raise(this, new TransferCompletedEvent
            {
                SessionId = sessionId,
                VehicleId = veh,
                Total = total,
                Accepted = acc,
                Rejected = rej,
                Timestamp = DateTime.UtcNow
            });
            ConsoleUi.Info($"[END] Session {sessionId} ({reason})");
            ConsoleUi.SessionEnd(sessionId, total, acc, rej);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // Očisti sve otvorene sesije pri gašenju servisa
            List<int> toClose;
            lock (_gate)
            {
                toClose = _sessions.Keys.ToList();
            }
            foreach (var sid in toClose) SafeEndSession(sid, "service disposing");
        }
    }
}
