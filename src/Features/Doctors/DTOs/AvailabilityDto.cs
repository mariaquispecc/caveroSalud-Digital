using System;

namespace CaveroSalud.Features.Doctors.DTOs
{
    public class AvailabilityDto
    {
        public Guid? Id { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
    }
}
