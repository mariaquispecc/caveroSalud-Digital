using System;

namespace CaveroSalud.Features.Laboratory.DTOs
{
    public class LabOrderDto
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public string TestName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
