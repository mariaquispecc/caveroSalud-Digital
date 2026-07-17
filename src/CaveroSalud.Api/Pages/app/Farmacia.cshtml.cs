using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Api.Pages.App
{
    [Authorize(Roles = "Farmacéutico,Farmaceutico")]
    public class FarmaciaModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CaveroDbContext _db;

        public FarmaciaModel(UserManager<ApplicationUser> userManager, CaveroDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public string UserFullName { get; set; } = string.Empty;

        public (int PendingOrders, int CriticalStock, int Shipped) Stats { get; set; }

        public List<RecentRequestRow> RecentRequests { get; set; } = new();

        public List<NotificationItem> Notifications { get; set; } = new();

        [BindProperty]
        public AccountInput Account { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        [TempData]
        public string? NotificationDetailTitle { get; set; }

        [TempData]
        public string? NotificationDetailBody { get; set; }

        public async Task OnGetAsync()
        {
            await LoadDashboardDataAsync();
        }

        public async Task<IActionResult> OnPostDeliverAsync(Guid prescriptionId, string? deliveryNotes)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                ErrorMessage = "No se pudo identificar al usuario actual.";
                return RedirectToPage();
            }

            var prescription = await _db.Prescriptions
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == prescriptionId);

            if (prescription == null)
            {
                ErrorMessage = "La receta ya no existe.";
                return RedirectToPage();
            }

            if (prescription.Status != PrescriptionStatuses.Requested)
            {
                ErrorMessage = "La receta ya no está pendiente de entrega.";
                return RedirectToPage();
            }

            foreach (var item in prescription.Items)
            {
                var inventoryItem = await _db.InventoryItems.FirstOrDefaultAsync(i => i.Name == item.Medication);
                if (inventoryItem == null)
                {
                    ErrorMessage = $"No existe inventario para '{item.Medication}'.";
                    return RedirectToPage();
                }

                if (inventoryItem.Quantity < item.Quantity)
                {
                    ErrorMessage = $"Stock insuficiente para '{item.Medication}'.";
                    return RedirectToPage();
                }

                inventoryItem.Quantity -= item.Quantity;
            }

            prescription.Status = PrescriptionStatuses.Delivered;
            prescription.DeliveredById = currentUser.Id;
            prescription.DeliveredAt = DateTime.UtcNow;
            prescription.DeliveryNotes = string.IsNullOrWhiteSpace(deliveryNotes)
                ? prescription.DeliveryNotes
                : deliveryNotes.Trim();

            await _db.UserNotifications.AddAsync(new UserNotification
            {
                Id = Guid.NewGuid(),
                UserId = prescription.PatientId,
                SenderUserId = currentUser.Id,
                Title = "Medicamentos despachados",
                Message = "Tu receta fue despachada en farmacia. Puedes acercarte a recogerla o revisar instrucciones.",
                Type = "success",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                SourceKey = $"prescription:{prescription.Id}"
            });

            await _db.SaveChangesAsync();

            SuccessMessage = "Entrega registrada correctamente.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAccountAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ErrorMessage = "No se pudo identificar al usuario actual.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(Account.FullName) || string.IsNullOrWhiteSpace(Account.Email))
            {
                ErrorMessage = "Nombre y correo son obligatorios.";
                await LoadDashboardDataAsync();
                return Page();
            }

            var normalizedEmail = Account.Email.Trim();
            var owner = await _userManager.FindByEmailAsync(normalizedEmail);
            if (owner != null && owner.Id != user.Id)
            {
                ErrorMessage = "El correo ya está en uso por otro usuario.";
                await LoadDashboardDataAsync();
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
                await LoadDashboardDataAsync();
                return Page();
            }

            SuccessMessage = "Tu cuenta se actualizó correctamente.";
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

        private async Task LoadDashboardDataAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            UserFullName = user?.FullName ?? user?.UserName ?? "Farmacéutico";

            if (user != null)
            {
                Account = new AccountInput
                {
                    FullName = user.FullName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Phone = user.PhoneNumber ?? string.Empty,
                    Dni = user.Dni ?? string.Empty
                };
            }

            // Stats
            var pendingCount = await _db.Prescriptions.CountAsync(p => p.Status == PrescriptionStatuses.Requested);
            var lowStockCount = await _db.InventoryItems.CountAsync(i => i.Quantity <= i.MinThreshold);
            var shippedToday = await _db.Prescriptions.CountAsync(p => p.Status == PrescriptionStatuses.Delivered && p.DeliveredAt >= DateTime.UtcNow.Date);

            Stats = (PendingOrders: pendingCount, CriticalStock: lowStockCount, Shipped: shippedToday);

            // Recent requests: load latest pending prescriptions and flatten items
            var prescriptions = await _db.Prescriptions
                .Where(p => p.Status == PrescriptionStatuses.Requested || p.Status == PrescriptionStatuses.Delivered)
                .OrderByDescending(p => p.CreatedAt)
                .Include(p => p.Items)
                .Take(20)
                .ToListAsync();

            var patientIds = prescriptions.Select(p => p.PatientId).Distinct().ToList();
            var patients = await _db.Users
                .Where(u => patientIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName })
                .ToDictionaryAsync(u => u.Id, u => u.FullName ?? "Paciente");

            var rows = new List<RecentRequestRow>();
            foreach (var p in prescriptions)
            {
                var patientName = patients.TryGetValue(p.PatientId, out var fullName) ? fullName : "Paciente";
                var whenSpan = DateTime.UtcNow - p.CreatedAt;
                string whenText = whenSpan.TotalHours < 24 ? ((int)whenSpan.TotalHours) + "h" : p.CreatedAt.ToShortDateString();

                foreach (var item in p.Items)
                {
                    rows.Add(new RecentRequestRow
                    {
                        PrescriptionId = p.Id,
                        Medicine = item.Medication,
                        Dosage = item.Dosage,
                        Quantity = item.Quantity,
                        Patient = patientName,
                        When = whenText,
                        Status = p.Status,
                        IsDelivered = p.Status == PrescriptionStatuses.Delivered
                    });
                }
            }

            RecentRequests = rows;

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

            if (pendingCount > 0)
            {
                Notifications.Add(new NotificationItem
                {
                    Type = "warning",
                    Message = $"Tienes {pendingCount} recetas pendientes de despacho.",
                    Body = "Revisa la tabla de solicitudes para confirmar entregas.",
                    When = "Ahora"
                });
            }

            if (lowStockCount > 0)
            {
                Notifications.Add(new NotificationItem
                {
                    Type = "warning",
                    Message = $"Hay {lowStockCount} medicamentos con stock crítico.",
                    Body = "Gestiona stock mínimo desde inventario.",
                    When = "Ahora"
                });
            }

            var deliveredLatest = await _db.Prescriptions.AsNoTracking()
                .Where(p => p.Status == PrescriptionStatuses.Delivered)
                .OrderByDescending(p => p.DeliveredAt)
                .Take(1)
                .FirstOrDefaultAsync();

            if (deliveredLatest != null && deliveredLatest.DeliveredAt.HasValue)
            {
                Notifications.Add(new NotificationItem
                {
                    Type = "info",
                    Message = "Último despacho registrado correctamente.",
                    Body = "El último despacho se registró sin incidencias.",
                    When = deliveredLatest.DeliveredAt.Value.ToLocalTime().ToString("dd/MM HH:mm")
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

        public class RecentRequestRow
        {
            public Guid PrescriptionId { get; set; }
            public string Medicine { get; set; } = string.Empty;
            public string Dosage { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public string Patient { get; set; } = string.Empty;
            public string When { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public bool IsDelivered { get; set; }
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
        }
    }
}
