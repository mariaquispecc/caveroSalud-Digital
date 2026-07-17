using System;

namespace CaveroSalud.Domain.Entities
{
    public class ContactMessage
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
