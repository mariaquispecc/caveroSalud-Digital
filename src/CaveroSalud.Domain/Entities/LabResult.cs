using System;

namespace CaveroSalud.Domain.Entities
{
    public class LabResult
    {
        public Guid Id { get; set; }
        public Guid LabOrderId { get; set; }
        public string Analyte { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
        public string ReferenceRange { get; set; }
        public string Comments { get; set; }
        public Guid? ValidatedBy { get; set; }
        public DateTime? ValidatedAt { get; set; }
        public bool Published { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
