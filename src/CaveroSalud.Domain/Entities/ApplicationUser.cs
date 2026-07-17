using System;
using Microsoft.AspNetCore.Identity;

namespace CaveroSalud.Domain.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FullName { get; set; }
        public string Dni { get; set; }
        public string PhoneNumber { get; set; }
        public string Speciality { get; set; }

        // Flags to control first-login flows
        public bool IsTemporaryPassword { get; set; }
        public bool FirstLoginCompleted { get; set; }
    }
}
