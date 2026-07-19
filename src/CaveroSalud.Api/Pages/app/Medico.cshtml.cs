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
    [Authorize(Roles = "Médico,Medico")]
    public class MedicoModel : PageModel
    {
        private readonly CaveroDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public MedicoModel(CaveroDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public bool ShowAllAgenda { get; set; }

        [BindProperty(SupportsGet = true)]
        public string AgendaSearch { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int AgendaPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int AgendaPageSize { get; set; } = 10;

        [BindProperty(SupportsGet = true)]
        public Guid? HistoryPatientId { get; set; }

        [BindProperty]
        public AttendInput Attend { get; set; } = new();

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

        public string UserFullName { get; set; } = string.Empty;

        public (int TodayAppointments, int AssignedPatients, int Urgent) Stats { get; set; }

        public List<AppointmentRow> Upcoming { get; set; } = new();

        public int AgendaTotalCount { get; set; }

        public int AgendaTotalPages { get; set; }

        public List<HistoryRow> PatientHistory { get; set; } = new();

        public string SelectedPatientName { get; set; } = string.Empty;

        public List<NotificationItem> Notifications { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostAttendAsync(Guid appointmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ErrorMessage = "No se pudo identificar al usuario actual.";
                return RedirectToPage();
            }

            var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId);
            if (appointment == null)
            {
                ErrorMessage = "La cita ya no existe.";
                return RedirectToPage(new { showAllAgenda = ShowAllAgenda, historyPatientId = HistoryPatientId });
            }

            if (appointment.DoctorId != user.Id)
            {
                ErrorMessage = "No puedes registrar una cita asignada a otro médico.";
                return RedirectToPage(new { showAllAgenda = ShowAllAgenda, historyPatientId = HistoryPatientId });
            }

            if (appointment.Status != AppointmentStatus.Scheduled)
            {
                ErrorMessage = "Solo se pueden atender citas en estado programado.";
                return RedirectToPage(new { showAllAgenda = ShowAllAgenda, historyPatientId = HistoryPatientId });
            }

            if (string.IsNullOrWhiteSpace(Attend.Diagnosis) || string.IsNullOrWhiteSpace(Attend.Treatment))
            {
                ErrorMessage = "Diagnóstico y tratamiento son obligatorios para registrar la atención.";
                await LoadAsync(user);
                return Page();
            }

            var record = new ClinicalRecord
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                PatientId = appointment.PatientId,
                DoctorId = user.Id,
                Diagnosis = Attend.Diagnosis.Trim(),
                Treatment = Attend.Treatment.Trim(),
                Observations = Attend.Observations?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                IsClosed = false
            };
            await _db.ClinicalRecords.AddAsync(record);

            if (!string.IsNullOrWhiteSpace(Attend.LabTestName))
            {
                await _db.LabOrders.AddAsync(new LabOrder
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointment.Id,
                    PatientId = appointment.PatientId,
                    DoctorId = user.Id,
                    TestName = Attend.LabTestName.Trim(),
                    Status = "Requested",
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!string.IsNullOrWhiteSpace(Attend.Medication) && Attend.MedicationQuantity > 0)
            {
                var prescription = new Prescription
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointment.Id,
                    PatientId = appointment.PatientId,
                    DoctorId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    Status = PrescriptionStatuses.Requested,
                    DeliveryNotes = string.Empty
                };
                prescription.Items.Add(new PrescriptionItem
                {
                    Id = Guid.NewGuid(),
                    Medication = Attend.Medication.Trim(),
                    Dosage = string.IsNullOrWhiteSpace(Attend.MedicationDosage) ? "Según indicación" : Attend.MedicationDosage.Trim(),
                    Quantity = Attend.MedicationQuantity
                });
                await _db.Prescriptions.AddAsync(prescription);
            }

            appointment.Status = AppointmentStatus.Completed;

            await _db.UserNotifications.AddAsync(new UserNotification
            {
                Id = Guid.NewGuid(),
                UserId = appointment.PatientId,
                SenderUserId = user.Id,
                Title = "Atención médica registrada",
                Message = $"Se registró tu atención de {appointment.Speciality}. Revisa tu historial para más detalle.",
                Type = "info",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                SourceKey = $"appointment:{appointment.Id}"
            });

            await _db.SaveChangesAsync();

            StatusMessage = "Atención médica registrada correctamente.";
            return RedirectToPage(new { showAllAgenda = ShowAllAgenda, historyPatientId = appointment.PatientId });
        }

        public async Task<IActionResult> OnPostUpdateAccountAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ErrorMessage = "No se pudo identificar al usuario actual.";
                return RedirectToPage(new { showAllAgenda = ShowAllAgenda, historyPatientId = HistoryPatientId, agendaSearch = AgendaSearch, agendaPage = AgendaPage, agendaPageSize = AgendaPageSize });
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
            return RedirectToPage("/app/medico");
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

            return RedirectToPage(new { showAllAgenda = ShowAllAgenda, historyPatientId = HistoryPatientId, agendaSearch = AgendaSearch, agendaPage = AgendaPage, agendaPageSize = AgendaPageSize });
        }

        private async Task LoadAsync(ApplicationUser? currentUser = null)
        {
            var user = currentUser ?? await _userManager.GetUserAsync(User);
            UserFullName = user?.FullName ?? user?.UserName ?? "Médico";

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
            var today = now.Date;
            var tomorrow = today.AddDays(1);

            var baseAppointments = _db.Appointments.AsNoTracking()
                .Where(a => a.DoctorId == user.Id)
                .OrderBy(a => a.StartAt);

            var scheduled = baseAppointments.Where(a => a.Status == AppointmentStatus.Scheduled);

            var todayAppointments = await scheduled.CountAsync(a => a.StartAt >= today && a.StartAt < tomorrow);
            var assignedPatients = await baseAppointments.Select(a => a.PatientId).Distinct().CountAsync();
            var urgent = await scheduled.CountAsync(a => a.StartAt >= now && a.StartAt <= now.AddHours(6));
            Stats = (TodayAppointments: todayAppointments, AssignedPatients: assignedPatients, Urgent: urgent);

            if (AgendaPage < 1)
            {
                AgendaPage = 1;
            }

            if (AgendaPageSize is < 5 or > 50)
            {
                AgendaPageSize = 10;
            }

            var agendaAppointments = await scheduled
                .Where(a => a.StartAt >= now.AddDays(-1))
                .Take(500)
                .ToListAsync();

            var patientIds = agendaAppointments.Select(a => a.PatientId).Distinct().ToList();
            var patientNames = await _db.Users.AsNoTracking()
                .Where(u => patientIds.Contains(u.Id))
                .Select(u => new { u.Id, Name = u.FullName ?? u.UserName ?? "Paciente" })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var mappedAgenda = agendaAppointments
                .Select(a => new AppointmentRow
                {
                    AppointmentId = a.Id,
                    PatientId = a.PatientId,
                    PatientName = patientNames.TryGetValue(a.PatientId, out var name) ? name : "Paciente",
                    Speciality = a.Speciality,
                    StartAt = a.StartAt,
                    Status = a.Status.ToString(),
                    CanAttend = a.Status == AppointmentStatus.Scheduled
                })
                .ToList();

            IEnumerable<AppointmentRow> filteredAgenda = mappedAgenda;
            if (!string.IsNullOrWhiteSpace(AgendaSearch))
            {
                var term = AgendaSearch.Trim();
                filteredAgenda = filteredAgenda.Where(a =>
                    a.PatientName.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || a.Speciality.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (!ShowAllAgenda)
            {
                filteredAgenda = filteredAgenda.Take(10);
            }

            var filteredList = filteredAgenda.OrderBy(a => a.StartAt).ToList();
            AgendaTotalCount = filteredList.Count;
            AgendaTotalPages = Math.Max(1, (int)Math.Ceiling(AgendaTotalCount / (double)AgendaPageSize));
            if (AgendaPage > AgendaTotalPages)
            {
                AgendaPage = AgendaTotalPages;
            }

            Upcoming = filteredList
                .Skip((AgendaPage - 1) * AgendaPageSize)
                .Take(AgendaPageSize)
                .ToList();

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

            var nearAppointments = agendaAppointments.Where(a => a.StartAt >= now && a.StartAt <= now.AddHours(24)).Take(3).ToList();
            foreach (var item in nearAppointments)
            {
                var patientName = patientNames.TryGetValue(item.PatientId, out var pName) ? pName : "Paciente";
                Notifications.Add(new NotificationItem
                {
                    Type = "warning",
                    Message = $"Cita próxima con {patientName} ({item.Speciality}).",
                    Body = $"Cita programada para {item.StartAt.ToLocalTime():dd/MM/yyyy HH:mm}.",
                    When = item.StartAt.ToLocalTime().ToString("dd/MM HH:mm")
                });
            }

            var validatedOrders = await _db.LabOrders.AsNoTracking()
                .Where(o => o.DoctorId == user.Id && o.Status == "Validated")
                .OrderByDescending(o => o.CreatedAt)
                .Take(3)
                .ToListAsync();

            foreach (var order in validatedOrders)
            {
                Notifications.Add(new NotificationItem
                {
                    Type = "info",
                    Message = $"Resultado validado para examen: {order.TestName}.",
                    Body = $"Se validó un resultado asociado al examen {order.TestName}.",
                    When = order.CreatedAt.ToLocalTime().ToString("dd/MM HH:mm")
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

            PatientHistory = new List<HistoryRow>();
            SelectedPatientName = string.Empty;
            if (HistoryPatientId.HasValue)
            {
                var hasContact = await baseAppointments.AnyAsync(a => a.PatientId == HistoryPatientId.Value)
                    || await _db.ClinicalRecords.AsNoTracking().AnyAsync(c => c.DoctorId == user.Id && c.PatientId == HistoryPatientId.Value);

                if (hasContact)
                {
                    SelectedPatientName = await _db.Users.AsNoTracking()
                        .Where(u => u.Id == HistoryPatientId.Value)
                        .Select(u => u.FullName ?? u.UserName ?? "Paciente")
                        .FirstOrDefaultAsync() ?? "Paciente";

                    PatientHistory = await _db.ClinicalRecords.AsNoTracking()
                        .Where(r => r.PatientId == HistoryPatientId.Value)
                        .OrderByDescending(r => r.CreatedAt)
                        .Take(15)
                        .Select(r => new HistoryRow
                        {
                            CreatedAt = r.CreatedAt,
                            Diagnosis = r.Diagnosis,
                            Treatment = r.Treatment,
                            IsClosed = r.IsClosed
                        })
                        .ToListAsync();
                }
            }
        }

        public class AppointmentRow
        {
            public Guid AppointmentId { get; set; }
            public Guid PatientId { get; set; }
            public string PatientName { get; set; } = string.Empty;
            public string Speciality { get; set; } = string.Empty;
            public DateTime StartAt { get; set; }
            public string Status { get; set; } = string.Empty;
            public bool CanAttend { get; set; }
        }

        public class HistoryRow
        {
            public DateTime CreatedAt { get; set; }
            public string Diagnosis { get; set; } = string.Empty;
            public string Treatment { get; set; } = string.Empty;
            public bool IsClosed { get; set; }
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

        public class AttendInput
        {
            public string Diagnosis { get; set; } = string.Empty;
            public string Treatment { get; set; } = string.Empty;
            public string? Observations { get; set; }
            public string? LabTestName { get; set; }
            public string? Medication { get; set; }
            public string? MedicationDosage { get; set; }
            public int MedicationQuantity { get; set; } = 1;
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
