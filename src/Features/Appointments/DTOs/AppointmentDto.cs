using System;

namespace CaveroSalud.Features.Appointments.DTOs
{
    public class AppointmentDto
    {
        public Guid Id { get; set; }
        public Guid? DoctorId { get; set; }
        public string Speciality { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string Status { get; set; }
    }
}
