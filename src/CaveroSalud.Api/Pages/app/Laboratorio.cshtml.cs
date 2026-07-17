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
    [Authorize(Roles = "Laboratorista")]
    public class LaboratorioModel : PageModel
    {
        private readonly CaveroDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public LaboratorioModel(CaveroDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [BindProperty]
        public ResultInput Result { get; set; } = new();

        [BindProperty]
        public InventoryInput Inventory { get; set; } = new();

        [BindProperty]
        public AccountInput Account { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string OrderSearch { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string OrderStatus { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int OrderPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int OrderPageSize { get; set; } = 10;

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        [TempData]
        public string? NotificationDetailTitle { get; set; }

        [TempData]
        public string? NotificationDetailBody { get; set; }

        public string UserFullName { get; set; } = string.Empty;

        public (int Pending, int Processed, int Errors) Stats { get; set; }

        public List<LabOrderRow> Orders { get; set; } = new();

        public int OrdersTotalCount { get; set; }

        public int OrdersTotalPages { get; set; }

        public List<InventoryItem> InventoryItems { get; set; } = new();

        public List<NotificationItem> Notifications { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostSubmitResultAsync(Guid orderId)
        {
            var order = await _db.LabOrders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                ErrorMessage = "La orden de laboratorio no existe.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(Result.Analyte))
            {
                ErrorMessage = "Debes indicar el analito del resultado.";
                await LoadAsync();
                return Page();
            }

            await _db.LabResults.AddAsync(new LabResult
            {
                Id = Guid.NewGuid(),
                LabOrderId = order.Id,
                Analyte = Result.Analyte.Trim(),
                Value = Result.Value?.Trim() ?? string.Empty,
                Unit = Result.Unit?.Trim() ?? string.Empty,
                ReferenceRange = Result.ReferenceRange?.Trim() ?? string.Empty,
                Comments = Result.Comments?.Trim() ?? string.Empty,
                Published = false,
                CreatedAt = DateTime.UtcNow
            });

            if (order.Status == "Requested")
            {
                order.Status = "Reported";
            }

            await _db.SaveChangesAsync();
            StatusMessage = "Resultado registrado correctamente.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostValidateAsync(Guid orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ErrorMessage = "No se pudo identificar al usuario actual.";
                return RedirectToPage();
            }

            var order = await _db.LabOrders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                ErrorMessage = "La orden de laboratorio no existe.";
                return RedirectToPage();
            }

            var results = await _db.LabResults.Where(r => r.LabOrderId == orderId).ToListAsync();
            if (results.Count == 0)
            {
                ErrorMessage = "No hay resultados para validar en esta orden.";
                return RedirectToPage();
            }

            foreach (var result in results)
            {
                result.ValidatedBy = user.Id;
                result.ValidatedAt = DateTime.UtcNow;
                result.Published = true;
            }

            order.Status = "Validated";

            await _db.UserNotifications.AddRangeAsync(new[]
            {
                new UserNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = order.DoctorId,
                    SenderUserId = user.Id,
                    Title = "Resultado validado",
                    Message = $"La orden de laboratorio '{order.TestName}' fue validada y publicada.",
                    Type = "info",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    SourceKey = $"laborder:{order.Id}"
                },
                new UserNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = order.PatientId,
                    SenderUserId = user.Id,
                    Title = "Resultados disponibles",
                    Message = $"Tus resultados del examen '{order.TestName}' ya están validados.",
                    Type = "success",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    SourceKey = $"laborder:{order.Id}"
                }
            });

            await _db.SaveChangesAsync();
            StatusMessage = "Resultados validados y publicados.";
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

            return RedirectToPage(new { orderSearch = OrderSearch, orderStatus = OrderStatus, orderPage = OrderPage, orderPageSize = OrderPageSize });
        }

        public async Task<IActionResult> OnPostSaveInventoryAsync()
        {
            if (string.IsNullOrWhiteSpace(Inventory.Name))
            {
                ErrorMessage = "El nombre del insumo es obligatorio.";
                await LoadAsync();
                return Page();
            }

            var name = Inventory.Name.Trim();
            var item = await _db.InventoryItems.FirstOrDefaultAsync(i => i.Name == name);
            if (item == null)
            {
                await _db.InventoryItems.AddAsync(new InventoryItem
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Unit = string.IsNullOrWhiteSpace(Inventory.Unit) ? "und" : Inventory.Unit.Trim(),
                    Quantity = Inventory.Quantity,
                    MinThreshold = Inventory.MinThreshold
                });
            }
            else
            {
                item.Unit = string.IsNullOrWhiteSpace(Inventory.Unit) ? item.Unit : Inventory.Unit.Trim();
                item.Quantity = Inventory.Quantity;
                item.MinThreshold = Inventory.MinThreshold;
            }

            await _db.SaveChangesAsync();
            StatusMessage = "Inventario de laboratorio actualizado.";
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

            StatusMessage = "Tu cuenta se actualizó correctamente.";
            return RedirectToPage();
        }

        private async Task LoadAsync(ApplicationUser? currentUser = null)
        {
            var user = currentUser ?? await _userManager.GetUserAsync(User);
            UserFullName = user?.FullName ?? user?.UserName ?? "Laboratorista";

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

            var today = DateTime.UtcNow.Date;

            if (OrderPage < 1)
            {
                OrderPage = 1;
            }

            if (OrderPageSize is < 5 or > 50)
            {
                OrderPageSize = 10;
            }

            var pending = await _db.LabOrders.AsNoTracking().CountAsync(o => o.Status == "Requested");
            var processed = await _db.LabResults.AsNoTracking().CountAsync(r => r.CreatedAt >= today);
            var alerts = await _db.InventoryItems.AsNoTracking().CountAsync(i => i.Quantity <= i.MinThreshold);
            Stats = (Pending: pending, Processed: processed, Errors: alerts);

            var orders = await _db.LabOrders.AsNoTracking()
                .OrderByDescending(o => o.CreatedAt)
                .Take(500)
                .ToListAsync();

            var patientIds = orders.Select(o => o.PatientId).Distinct().ToList();
            var patientNames = await _db.Users.AsNoTracking()
                .Where(u => patientIds.Contains(u.Id))
                .Select(u => new { u.Id, Name = u.FullName ?? u.UserName ?? "Paciente" })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var resultGroups = await _db.LabResults.AsNoTracking()
                .Where(r => orders.Select(o => o.Id).Contains(r.LabOrderId))
                .GroupBy(r => r.LabOrderId)
                .Select(g => new
                {
                    LabOrderId = g.Key,
                    Count = g.Count(),
                    IsValidated = g.Any(x => x.Published)
                })
                .ToListAsync();

            var resultMap = resultGroups.ToDictionary(x => x.LabOrderId, x => x);

            var mappedOrders = orders.Select(o =>
            {
                var hasResult = resultMap.TryGetValue(o.Id, out var info);
                return new LabOrderRow
                {
                    OrderId = o.Id,
                    PatientName = patientNames.TryGetValue(o.PatientId, out var name) ? name : "Paciente",
                    TestName = o.TestName,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt,
                    ResultCount = hasResult ? info!.Count : 0,
                    IsValidated = hasResult && info!.IsValidated
                };
            }).ToList();

            IEnumerable<LabOrderRow> filteredOrders = mappedOrders;
            if (!string.IsNullOrWhiteSpace(OrderSearch))
            {
                var term = OrderSearch.Trim();
                filteredOrders = filteredOrders.Where(o =>
                    o.PatientName.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || o.TestName.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(OrderStatus))
            {
                filteredOrders = filteredOrders.Where(o => o.Status.Equals(OrderStatus, StringComparison.OrdinalIgnoreCase));
            }

            var filteredList = filteredOrders.OrderByDescending(o => o.CreatedAt).ToList();
            OrdersTotalCount = filteredList.Count;
            OrdersTotalPages = Math.Max(1, (int)Math.Ceiling(OrdersTotalCount / (double)OrderPageSize));
            if (OrderPage > OrdersTotalPages)
            {
                OrderPage = OrdersTotalPages;
            }

            Orders = filteredList
                .Skip((OrderPage - 1) * OrderPageSize)
                .Take(OrderPageSize)
                .ToList();

            InventoryItems = await _db.InventoryItems.AsNoTracking()
                .OrderBy(i => i.Name)
                .Take(80)
                .ToListAsync();

            Notifications = new List<NotificationItem>();

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

            if (pending > 0)
            {
                Notifications.Add(new NotificationItem { Type = "warning", Message = $"Tienes {pending} órdenes pendientes por procesar.", Body = "Revisa las órdenes para mantener tiempos de respuesta.", When = "Ahora" });
            }

            var reportedPendingValidation = await _db.LabOrders.AsNoTracking().CountAsync(o => o.Status == "Reported");
            if (reportedPendingValidation > 0)
            {
                Notifications.Add(new NotificationItem { Type = "info", Message = $"Hay {reportedPendingValidation} órdenes con resultados por validar.", Body = "Ingresa a las órdenes y usa la acción validar.", When = "Ahora" });
            }

            if (alerts > 0)
            {
                Notifications.Add(new NotificationItem { Type = "warning", Message = $"Hay {alerts} insumos con stock crítico.", Body = "Puedes gestionar insumos desde el botón de inventario.", When = "Ahora" });
            }

            if (Notifications.Count == 0)
            {
                Notifications.Add(new NotificationItem { Type = "success", Message = "No tienes notificaciones nuevas.", Body = "No tienes notificaciones nuevas.", When = "Ahora" });
            }
        }

        public class LabOrderRow
        {
            public Guid OrderId { get; set; }
            public string PatientName { get; set; } = string.Empty;
            public string TestName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public int ResultCount { get; set; }
            public bool IsValidated { get; set; }
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

        public class ResultInput
        {
            public string Analyte { get; set; } = string.Empty;
            public string? Value { get; set; }
            public string? Unit { get; set; }
            public string? ReferenceRange { get; set; }
            public string? Comments { get; set; }
        }

        public class InventoryInput
        {
            public string Name { get; set; } = string.Empty;
            public string? Unit { get; set; }
            public decimal Quantity { get; set; }
            public decimal MinThreshold { get; set; }
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
