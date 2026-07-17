using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Features.ClinicalHistory.DTOs;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Features.ClinicalHistory.Controllers
{
    [ApiController]
    [Route("api/v1/clinical")]
    public class ClinicalHistoryController : ControllerBase
    {
        private readonly CaveroDbContext _db;

        public ClinicalHistoryController(CaveroDbContext db)
        {
            _db = db;
        }

        [HttpGet("my-history")]
        [Authorize(Roles = "Paciente")]
        public async Task<IActionResult> MyHistory()
        {
            var patientId = GetUserId();
            var records = await _db.ClinicalRecords
                .Where(r => r.PatientId == patientId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var result = records.Select(r => MapRecord(r));
            return Ok(result);
        }

        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Médico,Medico")]
        public async Task<IActionResult> PatientHistory(Guid patientId)
        {
            var doctorId = GetUserId();

            // Check doctor had at least one contact with patient (either appointment or clinical record)
            var hadContact = await _db.Appointments.AnyAsync(a => a.DoctorId == doctorId && a.PatientId == patientId)
                || await _db.ClinicalRecords.AnyAsync(c => c.DoctorId == doctorId && c.PatientId == patientId);

            if (!hadContact) return Forbid();

            var records = await _db.ClinicalRecords
                .Where(r => r.PatientId == patientId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var result = records.Select(r => MapRecord(r));
            return Ok(result);
        }

        [HttpPost("records/{id}/observations")]
        [Authorize(Roles = "Médico,Medico")]
        public async Task<IActionResult> AddObservation(Guid id, [FromBody] ObservationDto dto)
        {
            var doctorId = GetUserId();
            var record = await _db.ClinicalRecords.FindAsync(id);
            if (record == null) return NotFound();
            if (record.DoctorId != doctorId) return Forbid();
            if (record.IsClosed) return BadRequest("Record is closed and cannot be edited.");

            record.Observations = (record.Observations ?? string.Empty) + "\n" + dto.Observations;
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("records/{id}/close")]
        [Authorize(Roles = "Médico,Medico")]
        public async Task<IActionResult> CloseRecord(Guid id)
        {
            var doctorId = GetUserId();
            var record = await _db.ClinicalRecords.FindAsync(id);
            if (record == null) return NotFound();
            if (record.DoctorId != doctorId) return Forbid();
            record.IsClosed = true;
            await _db.SaveChangesAsync();
            return Ok();
        }

        private ClinicalRecordDto MapRecord(ClinicalRecord r)
        {
            var labs = _db.LabOrders.Where(l => l.AppointmentId == r.AppointmentId).Select(l => new { l.Id, l.TestName, l.Status, l.CreatedAt }).ToList();
            var pres = _db.Prescriptions.Where(p => p.AppointmentId == r.AppointmentId).Select(p => new { p.Id, p.CreatedAt, Items = p.Items.Select(i => new { i.Medication, i.Dosage, i.Quantity }) }).ToList();

            return new ClinicalRecordDto
            {
                Id = r.Id,
                CreatedAt = r.CreatedAt,
                DoctorId = r.DoctorId,
                Speciality = null,
                Diagnosis = r.Diagnosis,
                Treatment = r.Treatment,
                Observations = r.Observations,
                IsClosed = r.IsClosed,
                LabResults = labs,
                Prescriptions = pres
            };
        }

        private Guid GetUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Guid.Parse(sub);
        }
    }

    public class ObservationDto { public string Observations { get; set; } }
}
