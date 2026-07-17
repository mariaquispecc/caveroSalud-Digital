using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Features.Doctors.DTOs;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Features.Doctors.Controllers
{
    [ApiController]
    [Route("api/v1/doctor")]
    [Authorize(Roles = "Médico,Medico")]
    public class DoctorsController : ControllerBase
    {
        private readonly CaveroDbContext _db;

        public DoctorsController(CaveroDbContext db)
        {
            _db = db;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var doctorId = GetUserId();
            var today = DateTime.UtcNow.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

            var todayAppts = await _db.Appointments
                .Where(a => a.DoctorId == doctorId && a.StartAt.Date == today && a.Status == AppointmentStatus.Scheduled)
                .OrderBy(a => a.StartAt)
                .ToListAsync();

            var weekAppts = await _db.Appointments
                .Where(a => a.DoctorId == doctorId && a.StartAt.Date >= startOfWeek && a.StartAt.Date <= startOfWeek.AddDays(6) && a.Status == AppointmentStatus.Scheduled)
                .OrderBy(a => a.StartAt)
                .ToListAsync();

            var patientsSeen = await _db.ClinicalRecords
                .Where(c => c.DoctorId == doctorId)
                .Select(c => c.PatientId).Distinct().CountAsync();

            var prescriptionsIssued = await _db.Prescriptions.Where(p => p.DoctorId == doctorId).CountAsync();

            var dto = new DashboardDto
            {
                TodayAppointments = todayAppts.Select(a => new { a.Id, a.PatientId, a.StartAt, a.EndAt, a.Speciality }),
                WeekAppointments = weekAppts.Select(a => new { a.Id, a.PatientId, a.StartAt, a.EndAt, a.Speciality }),
                PatientsSeenCount = patientsSeen,
                PrescriptionsIssuedCount = prescriptionsIssued
            };

            return Ok(dto);
        }

        [HttpPost("availability")]
        public async Task<IActionResult> AddAvailability([FromBody] AvailabilityDto dto)
        {
            var doctorId = GetUserId();
            var utcStart = dto.StartAt.ToUniversalTime();
            var utcEnd = dto.EndAt.ToUniversalTime();

            if (utcEnd <= utcStart)
                return BadRequest("Availability end time must be after start time.");

            var overlap = await _db.DoctorAvailabilities
                .Where(d => d.DoctorId == doctorId && d.StartAt < utcEnd && utcStart < d.EndAt)
                .AnyAsync();
            if (overlap) return BadRequest("Availability overlaps with an existing availability slot.");

            var av = new DoctorAvailability
            {
                Id = Guid.NewGuid(),
                DoctorId = doctorId,
                StartAt = utcStart,
                EndAt = utcEnd
            };
            await _db.DoctorAvailabilities.AddAsync(av);
            await _db.SaveChangesAsync();
            return Ok(new { id = av.Id });
        }

        [HttpDelete("availability/{id}")]
        public async Task<IActionResult> RemoveAvailability(Guid id)
        {
            var doctorId = GetUserId();
            var av = await _db.DoctorAvailabilities.FindAsync(id);
            if (av == null) return NotFound();
            if (av.DoctorId != doctorId) return Forbid();
            _db.DoctorAvailabilities.Remove(av);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("attend/{appointmentId}")]
        public async Task<IActionResult> Attend(Guid appointmentId, [FromBody] AttendAppointmentDto dto)
        {
            var doctorId = GetUserId();
            var appt = await _db.Appointments.FindAsync(appointmentId);
            if (appt == null) return NotFound();
            if (appt.DoctorId != doctorId) return Forbid();
            if (appt.Status != AppointmentStatus.Scheduled)
                return BadRequest("Only scheduled appointments can be attended.");

            var record = new ClinicalRecord
            {
                Id = Guid.NewGuid(),
                AppointmentId = appt.Id,
                PatientId = appt.PatientId,
                DoctorId = doctorId,
                Diagnosis = dto.Diagnosis,
                Treatment = dto.Treatment,
                Observations = dto.Observations ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };
            await _db.ClinicalRecords.AddAsync(record);

            if (dto.LabOrders != null)
            {
                foreach (var lo in dto.LabOrders)
                {
                    var order = new LabOrder
                    {
                        Id = Guid.NewGuid(),
                        AppointmentId = appt.Id,
                        PatientId = appt.PatientId,
                        DoctorId = doctorId,
                        TestName = lo.TestName,
                        CreatedAt = DateTime.UtcNow,
                        Status = "Requested"
                    };
                    await _db.LabOrders.AddAsync(order);
                }
            }

            if (dto.Prescriptions != null)
            {
                var pres = new Prescription
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appt.Id,
                    PatientId = appt.PatientId,
                    DoctorId = doctorId,
                    CreatedAt = DateTime.UtcNow,
                    DeliveryNotes = string.Empty
                };
                foreach (var p in dto.Prescriptions)
                {
                    pres.Items.Add(new PrescriptionItem
                    {
                        Id = Guid.NewGuid(),
                        Medication = p.Medication,
                        Dosage = p.Dosage,
                        Quantity = p.Quantity
                    });
                }
                await _db.Prescriptions.AddAsync(pres);
            }

            appt.Status = AppointmentStatus.Completed;
            await _db.SaveChangesAsync();

            return Ok(new { recordId = record.Id });
        }

        private Guid GetUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Guid.Parse(sub);
        }
    }
}
