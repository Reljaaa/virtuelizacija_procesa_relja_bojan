using System.ServiceModel;

namespace Common
{
    [ServiceContract(SessionMode = SessionMode.Allowed)]
    public interface IChargingService
    {
        [OperationContract] StartSessionResponse StartSession(StartSessionRequest request);
        [OperationContract] void PushSample(int sessionId, SampleDto sample);
        [OperationContract] void EndSession(int sessionId);
    }
}



