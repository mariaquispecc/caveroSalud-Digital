using System;
using System.Collections.Generic;

namespace CaveroSalud.Features.Laboratory.DTOs
{
    public class SubmitResultsDto
    {
        public List<LabResultItemDto> Results { get; set; }
    }

    public class LabResultItemDto
    {
        public string Analyte { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
        public string ReferenceRange { get; set; }
        public string Comments { get; set; }
    }
}
