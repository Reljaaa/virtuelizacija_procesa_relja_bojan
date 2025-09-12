using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class StartSessionRequest
    {
        [DataMember(Order = 1, IsRequired = true)] 
        public string VehicleId { get; set; } = string.Empty;
    }
}
