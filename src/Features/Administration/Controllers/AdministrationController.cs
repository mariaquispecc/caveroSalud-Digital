using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Features.Administration.DTOs;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Features.Administration.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize(Roles = "Administrador")]
    public class AdministrationController : ControllerBase
    {
        private readonly CaveroDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public AdministrationController(
            CaveroDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _db.Users
                .Select(u => new AdminUserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Dni = u.Dni,
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    Speciality = u.Speciality,
                    IsActive = !u.LockoutEnabled || (u.LockoutEnd == null || u.LockoutEnd <= DateTime.UtcNow),
                    Role = _db.UserRoles
                                .Where(ur => ur.UserId == u.Id)
                                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                                .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] ManageUserDto dto)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null) return BadRequest("Email already registered.");

            var normalizedRole = NormalizeRole(dto.Role);
            var isMedicRole = normalizedRole.Equals("Médico", StringComparison.OrdinalIgnoreCase) || normalizedRole.Equals("Medico", StringComparison.OrdinalIgnoreCase);
            if (isMedicRole)
            {
                if (string.IsNullOrWhiteSpace(dto.Speciality))
                {
                    return BadRequest("Active speciality is required for medico role.");
                }

                var isActiveSpeciality = await _db.Specialities.AsNoTracking()
                    .AnyAsync(s => s.IsActive && s.Name == dto.Speciality);
                if (!isActiveSpeciality)
                {
                    return BadRequest("Selected speciality is not active.");
                }
            }
            else
            {
                dto.Speciality = string.Empty;
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                Dni = dto.Dni,
                PhoneNumber = dto.Phone,
                Speciality = dto.Speciality,
                IsTemporaryPassword = true,
                FirstLoginCompleted = false,
                LockoutEnabled = !dto.IsActive
            };

            var password = string.IsNullOrWhiteSpace(dto.TemporaryPassword)
                ? Guid.NewGuid().ToString("N").Substring(0, 12)
                : dto.TemporaryPassword;

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            if (!await _roleManager.RoleExistsAsync(normalizedRole))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(normalizedRole));
            }
            await _userManager.AddToRoleAsync(user, normalizedRole);

            return Ok(new { message = "User created", password });
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] ManageUserDto dto)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            var normalizedRole = NormalizeRole(dto.Role);
            var isMedicRole = normalizedRole.Equals("Médico", StringComparison.OrdinalIgnoreCase) || normalizedRole.Equals("Medico", StringComparison.OrdinalIgnoreCase);
            if (isMedicRole)
            {
                if (string.IsNullOrWhiteSpace(dto.Speciality))
                {
                    return BadRequest("Active speciality is required for medico role.");
                }

                var isActiveSpeciality = await _db.Specialities.AsNoTracking()
                    .AnyAsync(s => s.IsActive && s.Name == dto.Speciality);
                if (!isActiveSpeciality)
                {
                    return BadRequest("Selected speciality is not active.");
                }
            }
            else
            {
                dto.Speciality = string.Empty;
            }

            user.FullName = dto.FullName;
            user.Dni = dto.Dni;
            user.Email = dto.Email;
            user.UserName = dto.Email;
            user.PhoneNumber = dto.Phone;
            user.Speciality = dto.Speciality;
            user.LockoutEnabled = !dto.IsActive;
            if (!dto.IsActive) user.LockoutEnd = DateTime.UtcNow.AddYears(100);
            else user.LockoutEnd = null;

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Count == 0 || roles[0] != normalizedRole)
            {
                if (roles.Count > 0)
                {
                    await _userManager.RemoveFromRolesAsync(user, roles);
                }
                if (!await _roleManager.RoleExistsAsync(normalizedRole))
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(normalizedRole));
                }
                await _userManager.AddToRoleAsync(user, normalizedRole);
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded) return BadRequest(updateResult.Errors);

            return Ok(new { message = "User updated" });
        }

        [HttpPost("appointments/{id}/approve")]
        public async Task<IActionResult> ApproveAppointment(Guid id)
        {
            var appointment = await _db.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();
            if (appointment.Status != AppointmentStatus.Scheduled)
                return BadRequest("Appointment is not pending.");

            // No state change needed for aprobada if business model uses Scheduled as pending/approved.
            return Ok(new { message = "Appointment approved" });
        }

        [HttpPost("appointments/{id}/reschedule")]
        public async Task<IActionResult> RescheduleAppointment(Guid id, [FromBody] AppointmentActionDto dto)
        {
            var appointment = await _db.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();
            if (dto.NewStartAt == null || dto.NewEndAt == null)
                return BadRequest("New start and end times are required.");

            appointment.StartAt = dto.NewStartAt.Value.ToUniversalTime();
            appointment.EndAt = dto.NewEndAt.Value.ToUniversalTime();
            await _db.SaveChangesAsync();
            return Ok(new { message = "Appointment rescheduled" });
        }

        [HttpGet("specialities")]
        public async Task<IActionResult> GetSpecialities()
        {
            var specialities = await _db.Specialities
                .Select(s => new SpecialityDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    IsActive = s.IsActive
                })
                .ToListAsync();
            return Ok(specialities);
        }

        [HttpPost("specialities")]
        public async Task<IActionResult> CreateSpeciality([FromBody] SpecialityDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Speciality name is required.");

            var speciality = new Speciality
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                IsActive = dto.IsActive
            };
            await _db.Specialities.AddAsync(speciality);
            await _db.SaveChangesAsync();
            return Ok(new { id = speciality.Id });
        }

        [HttpPut("specialities/{id}")]
        public async Task<IActionResult> UpdateSpeciality(Guid id, [FromBody] SpecialityDto dto)
        {
            var speciality = await _db.Specialities.FindAsync(id);
            if (speciality == null) return NotFound();
            speciality.Name = dto.Name;
            speciality.Description = dto.Description;
            speciality.IsActive = dto.IsActive;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Speciality updated" });
        }

        [HttpGet("public-info")]
        public async Task<IActionResult> GetPublicInfo()
        {
            var info = await _db.PublicInfos.OrderByDescending(p => p.UpdatedAt).FirstOrDefaultAsync();
            if (info == null) return NotFound();
            return Ok(new PublicInfoDto
            {
                Id = info.Id,
                Title = info.Title,
                TagLine = info.TagLine,
                Description = info.Description,
                Address = info.Address,
                Email = info.Email,
                Phone = info.Phone
            });
        }

        [HttpPost("public-info")]
        public async Task<IActionResult> UpsertPublicInfo([FromBody] PublicInfoDto dto)
        {
            var info = await _db.PublicInfos.OrderByDescending(p => p.UpdatedAt).FirstOrDefaultAsync();
            if (info == null)
            {
                info = new PublicInfo { Id = Guid.NewGuid() };
                _db.PublicInfos.Add(info);
            }

            info.Title = dto.Title;
            info.TagLine = dto.TagLine;
            info.Description = dto.Description;
            info.Address = dto.Address;
            info.Email = dto.Email;
            info.Phone = dto.Phone;
            info.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new { message = "Public portal info updated" });
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            var todayAppointments = await _db.Appointments
                .CountAsync(a => a.StartAt >= today && a.StartAt < tomorrow && a.Status == AppointmentStatus.Scheduled);

            var activeStaff = await _db.Users
                .Where(u => u.LockoutEnabled == false || u.LockoutEnd == null || u.LockoutEnd <= DateTime.UtcNow)
                .CountAsync(u => u.Email != null && u.Email != string.Empty && u.UserName != null && u.UserName != string.Empty);

            var pendingAppointments = await _db.Appointments
                .CountAsync(a => a.Status == AppointmentStatus.Scheduled && a.StartAt > DateTime.UtcNow);

            var prescriptionsCount = await _db.Prescriptions.CountAsync();
            var labOrdersCount = await _db.LabOrders.CountAsync();
            var lowStockItems = await _db.InventoryItems.CountAsync(i => i.Quantity <= i.MinThreshold);

            return Ok(new AdminSummaryDto
            {
                TodayAppointments = todayAppointments,
                ActiveStaff = activeStaff,
                PendingAppointments = pendingAppointments,
                PrescriptionsCount = prescriptionsCount,
                LabOrdersCount = labOrdersCount,
                LowStockItems = lowStockItems
            });
        }

        private Guid GetUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Guid.Parse(sub);
        }

        private static string NormalizeRole(string role)
        {
            return role switch
            {
                "Medico" => "Médico",
                "Farmaceutico" => "Farmacéutico",
                _ => role
            };
        }
    }
}
