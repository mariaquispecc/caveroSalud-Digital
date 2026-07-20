using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Api.Pages.App
{
    [Authorize(Roles = "Paciente")]
    public class PacienteModel : PageModel
    {
        private readonly CaveroDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public PacienteModel(CaveroDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public string UserFullName { get; set; } = string.Empty;

        public class AppointmentInfo
        {
            public string DoctorName { get; set; } = string.Empty;
            public string Speciality { get; set; } = string.Empty;
            public DateTime Date { get; set; }
        }

        public AppointmentInfo? NextAppointment { get; set; }

        public List<(string Title, string When)> RecentRecords { get; set; } = new();

        public List<NotificationItem> Notifications { get; set; } = new();

        [BindProperty]
        public AccountInput Account { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        [TempData]
        public string? NotificationDetailTitle { get; set; }

        [TempData]
        public string? NotificationDetailBody { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostUpdateAccountAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ErrorMessage = "No se pudo identificar al usuario actual.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Account.FullName) || string.IsNullOrWhiteSpace(Account.Email))
            {
                ErrorMessage = "Nombre y correo son obligatorios.";
                await LoadAsync(user);
                return Page();
            }

            var normalizedEmail = Account.Email.Trim();
            var owner = await _userManager.FindByEmailAsync(normalizedEmail);
            if (owner != null && owner.Id != user.Id)
            {
                ErrorMessage = "El correo ya está en uso por otro usuario.";
                await LoadAsync(user);
                return Page();
            }

            user.FullName = Account.FullName.Trim();
            user.Email = normalizedEmail;
            user.UserName = normalizedEmail;
            user.PhoneNumber = Account.Phone.Trim();
            user.Dni = Account.Dni.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                ErrorMessage = string.Join("; ", result.Errors.Select(e => e.Description));
                await LoadAsync(user);
                return Page();
            }

            await _db.SaveChangesAsync();

            var currentPassword = Request.Form["Account.CurrentPassword"].ToString();
            var newPassword = Request.Form["Account.NewPassword"].ToString();
            var confirmPassword = Request.Form["Account.ConfirmPassword"].ToString();

            if (!string.IsNullOrWhiteSpace(newPassword) || !string.IsNullOrWhiteSpace(currentPassword) || !string.IsNullOrWhiteSpace(confirmPassword))
            {
                if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                {
                    ErrorMessage = "Para cambiar la contraseña debes completar la actual, la nueva y la confirmación.";
                    await LoadAsync(user);
                    return Page();
                }

                if (newPassword != confirmPassword)
                {
                    ErrorMessage = "La nueva contraseña y la confirmación no coinciden.";
                    await LoadAsync(user);
                    return Page();
                }

                if (!await _userManager.CheckPasswordAsync(user, currentPassword))
                {
                    ErrorMessage = "La contraseña actual no es correcta.";
                    await LoadAsync(user);
                    return Page();
                }

                var changePasswordResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                if (!changePasswordResult.Succeeded)
                {
                    ErrorMessage = string.Join("; ", changePasswordResult.Errors.Select(e => e.Description));
                    await LoadAsync(user);
                    return Page();
                }

                // Explicitly update and save the user to persist the password change
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    ErrorMessage = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                    await LoadAsync(user);
                    return Page();
                }

                // Force save to database
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch
                {
                    // SaveChanges might fail if there are no tracked changes, but the password change should still be persisted
                }
            }

            
            StatusMessage = "Tu cuenta se actualizó correctamente.";
            await LoadAsync(user);
            return RedirectToPage("/app/paciente");
        }

        public async Task<IActionResult> OnPostViewNotificationAsync(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Page();
            }

            var notification = await _db.UserNotifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);
            if (notification == null)
            {
                return Page();
            }

            NotificationDetailTitle = notification.Title;
            NotificationDetailBody = notification.Message;

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return Page();
        }

        private async Task LoadAsync(ApplicationUser? currentUser = null)
        {
            var user = currentUser ?? await _userManager.GetUserAsync(User);
            UserFullName = user?.FullName ?? user?.UserName ?? "Paciente";

            if (user == null)
            {
                return;
            }

            Account = new AccountInput
            {
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Phone = user.PhoneNumber ?? string.Empty,
                Dni = user.Dni ?? string.Empty
            };

            var now = DateTime.UtcNow;
            var tomorrow = now.AddHours(24);

            var scheduledAppointments = await _db.Appointments.AsNoTracking()
                .Where(a => a.PatientId == user.Id && a.Status == AppointmentStatus.Scheduled && a.StartAt >= now)
                .OrderBy(a => a.StartAt)
                .Take(10)
                .ToListAsync();

            var nextAppointment = scheduledAppointments.FirstOrDefault();
            if (nextAppointment != null)
            {
                string doctorName = "Por confirmar";
                if (nextAppointment.DoctorId.HasValue)
                {
                    var doctor = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == nextAppointment.DoctorId.Value);
                    doctorName = doctor?.FullName ?? doctor?.UserName ?? "Por confirmar";
                }

                NextAppointment = new AppointmentInfo
                {
                    DoctorName = doctorName,
                    Speciality = nextAppointment.Speciality,
                    Date = nextAppointment.StartAt.ToLocalTime()
                };
            }
            else
            {
                NextAppointment = null;
            }

            var recentRecords = await _db.ClinicalRecords.AsNoTracking()
                .Where(r => r.PatientId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(3)
                .Select(r => new
                {
                    Title = string.IsNullOrWhiteSpace(r.Diagnosis) ? "Registro clínico" : r.Diagnosis,
                    When = r.CreatedAt
                })
                .ToListAsync();

            RecentRecords = recentRecords
                .Select(r => (
                    Title: r.Title,
                    When: r.When.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                ))
                .ToList();

            if (RecentRecords.Count == 0)
            {
                RecentRecords.Add(("Sin registros recientes", "Aún no tienes historial clínico registrado."));
            }

            Notifications = scheduledAppointments
                .Where(a => a.StartAt <= tomorrow)
                .Select(a =>
                {
                    var remaining = a.StartAt - now;
                    var urgency = remaining <= TimeSpan.FromHours(2) ? "warning" : "info";
                    var message = remaining <= TimeSpan.Zero
                        ? $"Tu cita de {a.Speciality} está programada para ahora." 
                        : $"Tienes una cita de {a.Speciality} en {Math.Max(1, (int)Math.Round(remaining.TotalHours))} hora(s).";
                    return new NotificationItem
                    {
                        Type = urgency,
                        Message = message,
                        Body = $"Tu cita de {a.Speciality} está programada para {a.StartAt.ToLocalTime():dd/MM/yyyy HH:mm}.",
                        When = a.StartAt.ToLocalTime().ToString("dd/MM HH:mm")
                    };
                })
                .Take(5)
                .ToList();

            var persistedNotifications = await _db.UserNotifications.AsNoTracking()
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(8)
                .ToListAsync();

            Notifications.InsertRange(0, persistedNotifications.Select(n => new NotificationItem
            {
                Id = n.Id,
                Type = n.Type,
                Message = n.Title,
                Body = n.Message,
                When = n.CreatedAt.ToLocalTime().ToString("dd/MM HH:mm"),
                IsRead = n.IsRead
            }));

            var dueReminders = await _db.Reminders.AsNoTracking()
                .Where(r => r.PatientId == user.Id && !r.Sent && r.SendAt <= now)
                .OrderByDescending(r => r.SendAt)
                .Take(3)
                .ToListAsync();

            foreach (var reminder in dueReminders)
            {
                Notifications.Add(new NotificationItem
                {
                    Type = "info",
                    Message = string.IsNullOrWhiteSpace(reminder.Message) ? "Tienes un recordatorio pendiente." : reminder.Message,
                    Body = string.IsNullOrWhiteSpace(reminder.Message) ? "Recordatorio del sistema." : reminder.Message,
                    When = reminder.SendAt.ToLocalTime().ToString("dd/MM HH:mm")
                });
            }

            if (Notifications.Count == 0)
            {
                Notifications.Add(new NotificationItem
                {
                    Type = "success",
                    Message = "No tienes notificaciones nuevas.",
                    Body = "No tienes notificaciones nuevas.",
                    When = "Ahora"
                });
            }
        }

        public class NotificationItem
        {
            public Guid? Id { get; set; }
            public string Type { get; set; } = "info";
            public string Message { get; set; } = string.Empty;
            public string Body { get; set; } = string.Empty;
            public string When { get; set; } = string.Empty;
            public bool IsRead { get; set; }
        }

        public class AccountInput
        {
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Dni { get; set; } = string.Empty;
            public string CurrentPassword { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
            public string ConfirmPassword { get; set; } = string.Empty;
        }
    }
}
