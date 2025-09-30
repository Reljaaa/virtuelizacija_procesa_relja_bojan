using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class ValidationFault
    {
        [DataMember(Order = 1)]
        public string Message { get; set; } = string.Empty;

        [DataMember(Order = 2)]
        public int RowIndex { get; set; }

        [DataMember(Order = 3)]
        public string FieldName { get; set; } = string.Empty;
    }
}
