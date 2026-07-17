using System;

namespace CaveroSalud.Domain.Entities
{
    public class LabOrder
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public string TestName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
    }
}
