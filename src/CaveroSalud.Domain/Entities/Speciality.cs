using System;

namespace CaveroSalud.Domain.Entities
{
    public class Speciality
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
