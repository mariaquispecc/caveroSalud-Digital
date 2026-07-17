using System.Threading.Tasks;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Api.Pages.Admin
{
    [Authorize(Roles = "Administrador,Admin")]
    public class ReportsModel : PageModel
    {
        private readonly CaveroDbContext _db;

        public ReportsModel(CaveroDbContext db)
        {
            _db = db;
        }

        public int UsersCount { get; set; }
        public int AppointmentsCount { get; set; }
        public int ContactMessagesCount { get; set; }
        public int PrescriptionsCount { get; set; }

        public async Task OnGetAsync()
        {
            UsersCount = await _db.Users.CountAsync();
            AppointmentsCount = await _db.Appointments.CountAsync();
            ContactMessagesCount = await _db.ContactMessages.CountAsync();
            PrescriptionsCount = await _db.Prescriptions.CountAsync();
        }
    }
}
