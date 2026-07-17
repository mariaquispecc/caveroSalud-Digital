using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Api.Pages.App
{
    [Authorize(Roles = "Administrador,Admin")]
    public class AdminReportsModel : PageModel
    {
        private readonly CaveroDbContext _db;

        public AdminReportsModel(CaveroDbContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public string FilterType { get; set; } = "day";

        [BindProperty(SupportsGet = true)]
        public DateTime? From { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? To { get; set; }

        public int UsersCount { get; set; }
        public int AppointmentsCount { get; set; }
        public int ContactMessagesCount { get; set; }
        public int PrescriptionsCount { get; set; }
        public List<ReportPoint> Points { get; set; } = new();

        public async Task OnGetAsync()
        {
            var from = From ?? DateTime.UtcNow.Date.AddDays(-30);
            var to = To ?? DateTime.UtcNow;

            UsersCount = await _db.Users.CountAsync();
            AppointmentsCount = await _db.Appointments.CountAsync(a => a.StartAt >= from && a.StartAt <= to);
            ContactMessagesCount = await _db.ContactMessages.CountAsync();
            PrescriptionsCount = await _db.Prescriptions.CountAsync();

            var appointments = await _db.Appointments.AsNoTracking()
                .Where(a => a.StartAt >= from && a.StartAt <= to)
                .ToListAsync();

            Points = appointments
                .GroupBy(a => FilterType == "year"
                    ? a.StartAt.ToString("yyyy")
                    : FilterType == "month"
                        ? a.StartAt.ToString("yyyy-MM")
                        : a.StartAt.ToString("yyyy-MM-dd"))
                .OrderBy(g => g.Key)
                .Select(g => new ReportPoint
                {
                    Label = g.Key,
                    Appointments = g.Count(),
                    Patients = g.Select(x => x.PatientId).Distinct().Count()
                })
                .ToList();
        }

        public class ReportPoint
        {
            public string Label { get; set; } = string.Empty;
            public int Appointments { get; set; }
            public int Patients { get; set; }
        }
    }
}
