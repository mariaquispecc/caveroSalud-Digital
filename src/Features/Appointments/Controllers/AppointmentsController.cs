using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Features.Appointments.DTOs;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Features.Appointments.Controllers
{
    [ApiController]
    [Route("api/v1/appointments")]
    public class AppointmentsController : ControllerBase
    {
        private readonly CaveroDbContext _db;

        public AppointmentsController(CaveroDbContext db)
        {
            _db = db;
        }

        [HttpGet("mine")]
        [Authorize(Roles = "Paciente")]
        public async Task<IActionResult> MyAppointments()
        {
            var userId = GetUserId();
            var items = await _db.Appointments
                .Where(a => a.PatientId == userId && a.Status == AppointmentStatus.Scheduled)
                .OrderBy(a => a.StartAt)
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    DoctorId = a.DoctorId,
                    Speciality = a.Speciality,
                    StartAt = a.StartAt,
                    EndAt = a.EndAt,
                    Status = a.Status.ToString()
                }).ToListAsync();

            return Ok(items);
        }

        [HttpPost]
        [Authorize(Roles = "Paciente")]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
        {
            var userId = GetUserId();

            if (string.IsNullOrWhiteSpace(dto.Speciality))
                return BadRequest("Speciality is required.");

            var speciality = await _db.Specialities.AsNoTracking()
                .FirstOrDefaultAsync(s => s.IsActive && s.Name == dto.Speciality);
            if (speciality == null)
                return BadRequest("Selected speciality is not active.");

            if (dto.EndAt <= dto.StartAt)
                return BadRequest("Appointment end time must be after start time.");

            var utcStart = dto.StartAt.ToUniversalTime();
            var utcEnd = dto.EndAt.ToUniversalTime();

            if (utcStart < DateTime.UtcNow)
                return BadRequest("Appointment cannot be created in the past.");

            if (dto.DoctorId.HasValue)
            {
                var avail = await _db.DoctorAvailabilities
                    .Where(d => d.DoctorId == dto.DoctorId.Value && d.StartAt <= utcStart && d.EndAt >= utcEnd)
                    .FirstOrDefaultAsync();

                if (avail == null) return BadRequest("Selected slot is not available for the chosen doctor.");

                var overlap = await _db.Appointments
                    .Where(a => a.DoctorId == dto.DoctorId.Value && a.Status == AppointmentStatus.Scheduled &&
                                a.StartAt < utcEnd && utcStart < a.EndAt)
                    .AnyAsync();
                if (overlap) return BadRequest("Selected slot is already booked.");
            }

            var appt = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = userId,
                DoctorId = dto.DoctorId,
                Speciality = speciality.Name,
                StartAt = utcStart,
                EndAt = utcEnd,
                Status = AppointmentStatus.Scheduled
            };

            await _db.Appointments.AddAsync(appt);

            var reminder = new Reminder
            {
                Id = Guid.NewGuid(),
                AppointmentId = appt.Id,
                PatientId = appt.PatientId,
                SendAt = appt.StartAt.AddHours(-24),
                Sent = false,
                Message = $"Recordatorio: tienes una cita de {appt.Speciality} el {appt.StartAt:u}."
            };

            await _db.Reminders.AddAsync(reminder);
            await _db.SaveChangesAsync();

            return Ok(new { id = appt.Id });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Paciente")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var userId = GetUserId();
            var appt = await _db.Appointments.FindAsync(id);
            if (appt == null) return NotFound();
            if (appt.PatientId != userId) return Forbid();
            if (appt.Status == AppointmentStatus.Cancelled) return BadRequest("Appointment is already cancelled.");
            if (appt.Status == AppointmentStatus.Completed) return BadRequest("Completed appointments cannot be cancelled.");
            if (appt.StartAt <= DateTime.UtcNow.AddHours(24))
                return BadRequest("Cancellations must be made at least 24 hours before the appointment.");

            appt.Status = AppointmentStatus.Cancelled;
            await _db.SaveChangesAsync();

            var rems = await _db.Reminders.Where(r => r.AppointmentId == appt.Id).ToListAsync();
            foreach (var r in rems) r.Sent = true;
            await _db.SaveChangesAsync();

            return Ok();
        }

        private Guid GetUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Guid.Parse(sub);
        }
    }
}
