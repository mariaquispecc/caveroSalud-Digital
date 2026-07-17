using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Api.Pages.App
{
    [Authorize(Roles = "Paciente")]
    public class PacienteHistorialModel : PageModel
    {
        private readonly CaveroDbContext _db;

        public PacienteHistorialModel(CaveroDbContext db)
        {
            _db = db;
        }

        public List<RecordRow> RecentRecords { get; set; } = new();

        public async Task OnGetAsync()
        {
            var patientId = GetUserId();

            RecentRecords = await _db.ClinicalRecords
                .Where(r => r.PatientId == patientId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new RecordRow
                {
                    CreatedAt = r.CreatedAt,
                    Diagnosis = r.Diagnosis ?? string.Empty,
                    Treatment = r.Treatment ?? string.Empty,
                    IsClosed = r.IsClosed
                })
                .Take(20)
                .ToListAsync();
        }

        private Guid GetUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Guid.Parse(sub ?? string.Empty);
        }

        public class RecordRow
        {
            public DateTime CreatedAt { get; set; }
            public string Diagnosis { get; set; } = string.Empty;
            public string Treatment { get; set; } = string.Empty;
            public bool IsClosed { get; set; }
        }
    }
}
