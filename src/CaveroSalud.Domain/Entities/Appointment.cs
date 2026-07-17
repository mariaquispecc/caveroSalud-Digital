using System;

namespace CaveroSalud.Domain.Entities
{
    public class Appointment
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public Guid? DoctorId { get; set; }
        public string Speciality { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public AppointmentStatus Status { get; set; }
    }

    public enum AppointmentStatus
    {
        Scheduled = 0,
        Cancelled = 1,
        Completed = 2
    }
}
