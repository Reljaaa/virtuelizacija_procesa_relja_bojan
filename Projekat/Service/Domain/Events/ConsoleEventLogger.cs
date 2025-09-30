using System;

namespace Service.Domain.Events
{
    public static class ConsoleEventLogger
    {
        public static void Init()
        {
            ChargingEventHub.TransferStarted += (s, e) => Console.WriteLine($"[EVT] TransferStarted #{e.SessionId} {e.VehicleId}");
            ChargingEventHub.SampleAccepted += (s, e) => Console.WriteLine($"[EVT] SampleAccepted #{e.SessionId} Row={e.RowIndex}");
            ChargingEventHub.SampleRejected += (s, e) => Console.WriteLine($"[EVT] SampleRejected #{e.SessionId} Row={e.RowIndex} {e.Field} {e.Message}");
            ChargingEventHub.TransferCompleted += (s, e) => Console.WriteLine($"[EVT] TransferCompleted #{e.SessionId} total={e.Total} accepted={e.Accepted} rejected={e.Rejected}");
            ChargingEventHub.WarningRaised += (s, e) => Console.WriteLine($"[EVT] Warning #{e.SessionId} {e.Code}: {e.Message}");
        }
    }
}

