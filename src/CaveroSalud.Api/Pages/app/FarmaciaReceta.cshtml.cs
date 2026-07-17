using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Api.Pages.App
{
    [Authorize(Roles = "Farmacéutico,Farmaceutico")]
    public class FarmaciaRecetaModel : PageModel
    {
        private readonly CaveroDbContext _db;

        public FarmaciaRecetaModel(CaveroDbContext db)
        {
            _db = db;
        }

        public PrescriptionDetail Detail { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid prescriptionId)
        {
            var prescription = await _db.Prescriptions
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == prescriptionId);

            if (prescription == null)
            {
                return NotFound();
            }

            var patientName = await _db.Users
                .Where(u => u.Id == prescription.PatientId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync() ?? "Paciente";

            var doctorName = await _db.Users
                .Where(u => u.Id == prescription.DoctorId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync() ?? "Médico";

            var deliveredBy = prescription.DeliveredById.HasValue
                ? await _db.Users
                    .Where(u => u.Id == prescription.DeliveredById.Value)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync()
                : null;

            Detail = new PrescriptionDetail
            {
                PrescriptionId = prescription.Id,
                AppointmentId = prescription.AppointmentId,
                CreatedAt = prescription.CreatedAt,
                Status = prescription.Status,
                PatientName = patientName,
                DoctorName = doctorName,
                DeliveredBy = deliveredBy ?? "-",
                DeliveredAt = prescription.DeliveredAt,
                DeliveryNotes = prescription.DeliveryNotes,
                Items = prescription.Items
                    .Select(i => new PrescriptionItemDetail
                    {
                        Medication = i.Medication,
                        Dosage = i.Dosage,
                        Quantity = i.Quantity
                    })
                    .ToList()
            };

            return Page();
        }

        public class PrescriptionDetail
        {
            public Guid PrescriptionId { get; set; }
            public Guid AppointmentId { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; } = string.Empty;
            public string PatientName { get; set; } = string.Empty;
            public string DoctorName { get; set; } = string.Empty;
            public string DeliveredBy { get; set; } = string.Empty;
            public DateTime? DeliveredAt { get; set; }
            public string? DeliveryNotes { get; set; }
            public List<PrescriptionItemDetail> Items { get; set; } = new();
        }

        public class PrescriptionItemDetail
        {
            public string Medication { get; set; } = string.Empty;
            public string Dosage { get; set; } = string.Empty;
            public int Quantity { get; set; }
        }
    }
}
