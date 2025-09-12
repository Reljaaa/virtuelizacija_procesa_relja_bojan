using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class SampleDto
    {
        [DataMember(Order = 1, IsRequired = true)] public DateTime Timestamp { get; set; }
        [DataMember(Order = 2)] public double VoltageRmsMin { get; set; }
        [DataMember(Order = 3)] public double VoltageRmsAvg { get; set; }
        [DataMember(Order = 4)] public double VoltageRmsMax { get; set; }
        
        [DataMember(Order = 5)] public double CurrentRmsMin { get; set; }
        [DataMember(Order = 6)] public double CurrentRmsAvg { get; set; }
        [DataMember(Order = 7)] public double CurrentRmsMax { get; set; }

        [DataMember(Order = 8)] public double RealPowerMin { get; set; }
        [DataMember(Order = 9)] public double RealPowerAvg { get; set; }
        [DataMember(Order = 10)] public double RealPowerMax { get; set; }

        [DataMember(Order = 11)] public double ReactivePowerMin { get; set; }
        [DataMember(Order = 12)] public double ReactivePowerAvg { get; set; }
        [DataMember(Order = 13)] public double ReactivePowerMax { get; set; }

        [DataMember(Order = 14)] public double ApparentPowerMin { get; set; }
        [DataMember(Order = 15)] public double ApparentPowerAvg { get; set; }
        [DataMember(Order = 16)] public double ApparentPowerMax { get; set; }

        [DataMember(Order = 17)] public double FrequencyMin { get; set; }
        [DataMember(Order = 18)] public double FrequencyAvg { get; set; }
        [DataMember(Order = 19)] public double FrequencyMax { get; set; }

        [DataMember (Order = 20, IsRequired = true)] public int RowIndex { get; set; }
        [DataMember(Order = 21, IsRequired = true)] public string VehicleId { get; set; } = string.Empty;

    }
}
