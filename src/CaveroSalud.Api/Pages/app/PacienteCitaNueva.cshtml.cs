using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Api.Pages.App
{
    [Authorize(Roles = "Paciente")]
    public class PacienteCitaNuevaModel : PageModel
    {
        private readonly CaveroDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public PacienteCitaNuevaModel(CaveroDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [BindProperty]
        public Guid? SelectedSpecialityId { get; set; }

        [BindProperty]
        public Guid? DoctorId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        public DateTime? StartAt { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
        public DateTime? EndAt { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public string SelectedSpecialityName { get; set; } = string.Empty;
        public List<SelectListItem> SpecialityOptions { get; set; } = new();
        public List<SelectListItem> DoctorOptions { get; set; } = new();
        public List<ApplicationUser> AvailableDoctors { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadLookupsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadLookupsAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (SelectedSpecialityId == null)
            {
                ModelState.AddModelError(string.Empty, "Selecciona una especialidad registrada.");
                return Page();
            }

            var speciality = await _db.Specialities.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == SelectedSpecialityId.Value && s.IsActive);

            if (speciality == null)
            {
                ModelState.AddModelError(string.Empty, "La especialidad seleccionada no está disponible.");
                return Page();
            }

            var doctors = await GetDoctorsBySpecialityAsync(speciality.Name);
            if (doctors.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "No hay médicos activos para esta especialidad.");
                return Page();
            }

            if (DoctorId == null)
            {
                DoctorId = doctors.Count == 1 ? doctors[0].Id : null;
            }

            if (DoctorId == null)
            {
                ModelState.AddModelError(string.Empty, "Selecciona un médico para esta especialidad.");
                return Page();
            }

            var chosenDoctor = doctors.FirstOrDefault(d => d.Id == DoctorId.Value);
            if (chosenDoctor == null)
            {
                ModelState.AddModelError(string.Empty, "El médico seleccionado no corresponde a la especialidad.");
                return Page();
            }

            if (StartAt >= EndAt)
            {
                ModelState.AddModelError(string.Empty, "La fecha de fin debe ser mayor que la fecha de inicio.");
                return Page();
            }

            var userId = GetUserId();
            var utcStart = StartAt.Value.ToUniversalTime();
            var utcEnd = EndAt.Value.ToUniversalTime();

            if (utcStart < DateTime.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "La cita no puede programarse en el pasado.");
                return Page();
            }

            var overlap = await _db.Appointments
                .Where(a => a.DoctorId == chosenDoctor.Id && a.Status == AppointmentStatus.Scheduled && a.StartAt < utcEnd && utcStart < a.EndAt)
                .AnyAsync();
            if (overlap)
            {
                ModelState.AddModelError(string.Empty, "El médico ya tiene una cita en ese horario.");
                return Page();
            }

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = userId,
                DoctorId = chosenDoctor.Id,
                Speciality = speciality.Name,
                StartAt = utcStart,
                EndAt = utcEnd,
                Status = AppointmentStatus.Scheduled
            };

            await _db.Appointments.AddAsync(appointment);
            await _db.Reminders.AddAsync(new Reminder
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                PatientId = userId,
                SendAt = utcStart.AddHours(-24),
                Sent = false,
                Message = $"Recordatorio: tienes una cita de {speciality.Name} el {utcStart:u}."
            });
            await _db.SaveChangesAsync();

            SuccessMessage = "Cita agendada correctamente.";
            return Page();
        }

        private async Task LoadLookupsAsync()
        {
            var specialities = await _db.Specialities.AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            SpecialityOptions = specialities
                .Select(s => new SelectListItem(s.Name, s.Id.ToString(), SelectedSpecialityId == s.Id))
                .ToList();

            if (SelectedSpecialityId == null && specialities.Count > 0)
            {
                SelectedSpecialityId = specialities[0].Id;
            }

            var selectedSpeciality = specialities.FirstOrDefault(s => s.Id == SelectedSpecialityId);
            SelectedSpecialityName = selectedSpeciality?.Name ?? string.Empty;

            AvailableDoctors = selectedSpeciality == null
                ? new List<ApplicationUser>()
                : await GetDoctorsBySpecialityAsync(selectedSpeciality.Name);

            DoctorOptions = AvailableDoctors
                .Select(d => new SelectListItem(
                    string.IsNullOrWhiteSpace(d.FullName) ? d.UserName ?? d.Id.ToString() : d.FullName,
                    d.Id.ToString(),
                    DoctorId == d.Id))
                .ToList();

            if (DoctorOptions.Count == 1 && DoctorId == null)
            {
                DoctorId = AvailableDoctors[0].Id;
            }
        }

        private async Task<List<ApplicationUser>> GetDoctorsBySpecialityAsync(string specialityName)
        {
            var doctors = await _userManager.GetUsersInRoleAsync("Médico");
            var medicos = await _userManager.GetUsersInRoleAsync("Medico");

            return doctors
                .Concat(medicos)
                .GroupBy(u => u.Id)
                .Select(g => g.First())
                .Where(u => string.Equals(u.Speciality, specialityName, StringComparison.OrdinalIgnoreCase))
                .Where(u => u.LockoutEnd == null || u.LockoutEnd <= DateTime.UtcNow)
                .OrderBy(u => u.FullName ?? u.UserName)
                .ToList();
        }

        private Guid GetUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(sub, out var userId))
            {
                throw new InvalidOperationException("No se pudo identificar al paciente autenticado.");
            }

            return userId;
        }
    }
}
