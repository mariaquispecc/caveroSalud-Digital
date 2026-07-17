using System;

namespace CaveroSalud.Domain.Entities
{
    public class DoctorAvailability
    {
        public Guid Id { get; set; }
        public Guid DoctorId { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
    }
}
