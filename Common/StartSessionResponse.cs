using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class StartSessionResponse
    {
        [DataMember(Order = 1, IsRequired = true)]
        public int SessionId { get; set; }
    }
}
