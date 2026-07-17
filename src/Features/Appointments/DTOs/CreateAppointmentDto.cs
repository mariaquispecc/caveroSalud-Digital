using System;

namespace CaveroSalud.Features.Appointments.DTOs
{
    public class CreateAppointmentDto
    {
        public string Speciality { get; set; }
        public Guid? DoctorId { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
    }
}
