using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [ServiceContract]
    public interface IChargingService
    {
        [OperationContract]
        StartSessionResponse StartSession(StartSessionRequest request);

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        void PushSample(int sessionId, SampleDto sample);

        [OperationContract]
        void EndSession(int sessionId);
    }
}
