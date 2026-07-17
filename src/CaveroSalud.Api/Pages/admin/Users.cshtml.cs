using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Api.Pages.Admin
{
    [Authorize(Roles = "Administrador")]
    public class UsersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public int TotalUsers { get; set; }
        public int PatientUsers { get; set; }
        public int StaffUsers { get; set; }
        public List<UserRow> Users { get; set; } = new();

        public async Task OnGetAsync()
        {
            var query = _userManager.Users.AsNoTracking();
            TotalUsers = await query.CountAsync();
            PatientUsers = await query.CountAsync(u => u.UserName != null && u.UserName.Contains("@"));
            StaffUsers = await query.CountAsync(u => u.Speciality != null && u.Speciality != string.Empty);

            Users = await query
                .OrderBy(u => u.FullName)
                .Take(20)
                .Select(u => new UserRow
                {
                    FullName = u.FullName ?? u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    Phone = u.PhoneNumber ?? string.Empty,
                    PrimaryRole = string.IsNullOrWhiteSpace(u.Speciality) ? "Paciente" : "Profesional"
                })
                .ToListAsync();
        }

        public class UserRow
        {
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string PrimaryRole { get; set; } = string.Empty;
        }
    }
}
