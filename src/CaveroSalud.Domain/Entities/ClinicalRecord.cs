using System;

namespace CaveroSalud.Domain.Entities
{
    public class ClinicalRecord
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid? AppointmentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string Treatment { get; set; } = string.Empty;
        public string Observations { get; set; } = string.Empty;
        public bool IsClosed { get; set; }
    }
}
