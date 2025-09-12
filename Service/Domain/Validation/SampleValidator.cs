using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Domain.Validation
{
    public static class SampleValidator
    {
        public static ValidationFault ValidateSample(SampleDto s)
        {
            if (s.Timestamp == default) 
                return new ValidationFault
                {
                    Message = "Timestamp je obavezan",
                    RowIndex = s.RowIndex,
                    FieldName = nameof(SampleDto.Timestamp) 
                };

        
            if (string.IsNullOrWhiteSpace(s.VehicleId))
                return new ValidationFault
                {
                    Message = "VehicleId je obavezan u SampleDto.",
                    RowIndex = s.RowIndex,
                    FieldName = nameof(SampleDto.VehicleId)
                };

            if (s.FrequencyAvg <= 0)
                return new ValidationFault
                {
                    Message = "FrequencyAvg mora biti > 0.",
                    RowIndex = s.RowIndex,
                    FieldName = nameof(SampleDto.FrequencyAvg)
                };

            if (s.VoltageRmsAvg <= 0)
                return new ValidationFault
                {
                    Message = "VoltageRmsAvg mora biti > 0.",
                    RowIndex = s.RowIndex,
                    FieldName = nameof(SampleDto.VoltageRmsAvg)
                };

            if (s.CurrentRmsAvg < 0)
                return new ValidationFault
                {
                    Message = "CurrentRmsAvg ne može biti negativan.",
                    RowIndex = s.RowIndex,
                    FieldName = nameof(SampleDto.CurrentRmsAvg)
                };

            return null;
        }
    }
}
