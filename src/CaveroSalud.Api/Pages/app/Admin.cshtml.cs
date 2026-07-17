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
    [Authorize(Roles = "Administrador")]
    public class AdminModel : PageModel
    {
        private readonly CaveroDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminModel(CaveroDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public string UserFullName { get; set; } = string.Empty;
        public IList<string> Roles { get; set; } = new List<string>();

        public (int UsersCount, int TodayAppointments, int Messages, int Alerts) Stats { get; set; }

        public List<(string Title, string Description, string When)> RecentActivity { get; set; } = new();

        public List<NotificationItem> Notifications { get; set; } = new();

        public List<string> AvailableTargetRoles { get; set; } = new() { "Paciente", "Médico", "Laboratorista", "Farmacéutico", "Administrador" };

        [BindProperty]
        public MessageInput MessageForm { get; set; } = new();

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

        public async Task<IActionResult> OnPostSendMessageAsync()
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null)
            {
                ErrorMessage = "No se pudo identificar al administrador.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(MessageForm.Title) || string.IsNullOrWhiteSpace(MessageForm.Body))
            {
                ErrorMessage = "Debes completar título y mensaje.";
                await LoadAsync(admin);
                return Page();
            }

            var recipients = await ResolveRecipientsAsync();
            if (recipients.Count == 0)
            {
                ErrorMessage = "No se encontraron destinatarios para el envío.";
                await LoadAsync(admin);
                return Page();
            }

            var now = DateTime.UtcNow;
            var notifications = recipients.Select(userId => new UserNotification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SenderUserId = admin.Id,
                Title = MessageForm.Title.Trim(),
                Message = MessageForm.Body.Trim(),
                Type = "message",
                IsRead = false,
                CreatedAt = now
            });

            await _db.UserNotifications.AddRangeAsync(notifications);
            await _db.SaveChangesAsync();

            StatusMessage = $"Mensaje enviado a {recipients.Count} usuario(s).";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostViewNotificationAsync(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage();
            }

            var notification = await _db.UserNotifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);
            if (notification == null)
            {
                return RedirectToPage();
            }

            NotificationDetailTitle = notification.Title;
            NotificationDetailBody = notification.Message;

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        private async Task LoadAsync(ApplicationUser? currentUser = null)
        {
            var user = currentUser ?? await _userManager.GetUserAsync(User);
            if (user != null)
            {
                UserFullName = user.FullName ?? user.UserName;
                Roles = await _userManager.GetRolesAsync(user);
            }

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var usersCount = await _db.Users.CountAsync();
            var todayAppointments = await _db.Appointments
                .CountAsync(a => a.StartAt >= today && a.StartAt < tomorrow && a.Status == AppointmentStatus.Scheduled);
            var alerts = await _db.InventoryItems.CountAsync(i => i.Quantity <= i.MinThreshold);

            var unreadForAdmin = user == null
                ? 0
                : await _db.UserNotifications.AsNoTracking().CountAsync(n => n.UserId == user.Id && !n.IsRead);

            Stats = (UsersCount: usersCount, TodayAppointments: todayAppointments, Messages: unreadForAdmin, Alerts: alerts);

            var nextAppointments = await _db.Appointments.AsNoTracking()
                .Where(a => a.StartAt >= DateTime.UtcNow && a.Status == AppointmentStatus.Scheduled)
                .OrderBy(a => a.StartAt)
                .Take(3)
                .Select(a => new { a.Speciality, a.StartAt })
                .ToListAsync();

            RecentActivity = nextAppointments
                .Select(a => (
                    Title: "Cita programada",
                    Description: $"Especialidad: {a.Speciality}",
                    When: a.StartAt.ToLocalTime().ToString("dd/MM HH:mm")
                ))
                .ToList();

            if (RecentActivity.Count == 0)
            {
                RecentActivity.Add(("Sin actividad", "No hay registros recientes para mostrar.", "--"));
            }

            Notifications = new List<NotificationItem>();

            if (user != null)
            {
                var userNotifications = await _db.UserNotifications.AsNoTracking()
                    .Where(n => n.UserId == user.Id)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(8)
                    .ToListAsync();

                Notifications.AddRange(userNotifications.Select(n => new NotificationItem
                {
                    Id = n.Id,
                    Type = n.Type,
                    Message = n.Title,
                    Body = n.Message,
                    When = n.CreatedAt.ToLocalTime().ToString("dd/MM HH:mm"),
                    IsRead = n.IsRead
                }));
            }

            if (todayAppointments > 0)
            {
                Notifications.Add(new NotificationItem
                {
                    Type = "info",
                    Message = $"Hay {todayAppointments} citas programadas para hoy.",
                    When = "Hoy"
                });
            }

            if (alerts > 0)
            {
                Notifications.Add(new NotificationItem
                {
                    Type = "warning",
                    Message = $"Se detectaron {alerts} alertas de inventario crítico.",
                    When = "Ahora"
                });
            }

            if (Notifications.Count == 0)
            {
                Notifications.Add(new NotificationItem
                {
                    Type = "success",
                    Message = "No hay notificaciones nuevas.",
                    When = "Ahora"
                });
            }
        }

        private async Task<List<Guid>> ResolveRecipientsAsync()
        {
            IQueryable<ApplicationUser> query = _db.Users.AsNoTracking();

            if (string.Equals(MessageForm.Target, "role", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(MessageForm.TargetRole))
            {
                var role = MessageForm.TargetRole.Trim();
                var roleId = await _db.Roles
                    .Where(r => r.Name == role)
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync();

                if (roleId == Guid.Empty)
                {
                    return new List<Guid>();
                }

                var userIdsByRole = await _db.UserRoles
                    .Where(ur => ur.RoleId == roleId)
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .ToListAsync();

                return userIdsByRole;
            }

            if (string.Equals(MessageForm.Target, "user", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(MessageForm.TargetEmail))
            {
                var userId = await _db.Users
                    .Where(u => u.Email == MessageForm.TargetEmail.Trim())
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();

                return userId == Guid.Empty ? new List<Guid>() : new List<Guid> { userId };
            }

            return await query.Select(u => u.Id).ToListAsync();
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

        public class MessageInput
        {
            public string Target { get; set; } = "all";
            public string TargetRole { get; set; } = string.Empty;
            public string TargetEmail { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Body { get; set; } = string.Empty;
        }
    }
}
