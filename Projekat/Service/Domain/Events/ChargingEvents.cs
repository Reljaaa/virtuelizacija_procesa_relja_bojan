using System;

namespace Service.Domain.Events
{
    public static class ChargingEventHub
    {
        public static event EventHandler<TransferStartedEvent> TransferStarted;
        public static event EventHandler<SampleAcceptedEvent> SampleAccepted;
        public static event EventHandler<SampleRejectedEvent> SampleRejected;
        public static event EventHandler<TransferCompletedEvent> TransferCompleted;
        public static event EventHandler<WarningEvent> WarningRaised;

        public static void Raise(object sender, TransferStartedEvent e) => TransferStarted?.Invoke(sender, e);
        public static void Raise(object sender, SampleAcceptedEvent e) => SampleAccepted?.Invoke(sender, e);
        public static void Raise(object sender, SampleRejectedEvent e) => SampleRejected?.Invoke(sender, e);
        public static void Raise(object sender, TransferCompletedEvent e) => TransferCompleted?.Invoke(sender, e);
        public static void Raise(object sender, WarningEvent e) => WarningRaised?.Invoke(sender, e);
    }

    public sealed class TransferStartedEvent : EventArgs
    {
        public int SessionId { get; set; }
        public string VehicleId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public sealed class SampleAcceptedEvent : EventArgs
    {
        public int SessionId { get; set; }
        public int RowIndex { get; set; }
        public string VehicleId { get; set; }
    }

    public sealed class SampleRejectedEvent : EventArgs
    {
        public int SessionId { get; set; }
        public int RowIndex { get; set; }
        public string VehicleId { get; set; }
        public string Field { get; set; }
        public string Message { get; set; }
    }

    public sealed class TransferCompletedEvent : EventArgs
    {
        public int SessionId { get; set; }
        public string VehicleId { get; set; }
        public int Total { get; set; }
        public int Accepted { get; set; }
        public int Rejected { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public sealed class WarningEvent : EventArgs
    {
        public int SessionId { get; set; }
        public string VehicleId { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
