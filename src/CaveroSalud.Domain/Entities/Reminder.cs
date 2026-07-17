using System;

namespace CaveroSalud.Domain.Entities
{
    public class Reminder
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public DateTime SendAt { get; set; }
        public bool Sent { get; set; }
        public string Message { get; set; }
    }
}
