using System;

namespace CaveroSalud.Domain.Entities
{
    public class PublicInfo
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string TagLine { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
