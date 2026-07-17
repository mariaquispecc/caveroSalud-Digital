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
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Api.Pages.App
{
    [Authorize(Roles = "Administrador")]
    public class AdminUsersModel : PageModel
    {
        private static readonly string[] AllowedRoles =
        {
            "Administrador",
            "Médico",
            "Laboratorista",
            "Farmacéutico"
        };

        private readonly CaveroDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public AdminUsersModel(
            CaveroDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [BindProperty(SupportsGet = true)]
        public Guid? EditId { get; set; }

        [BindProperty]
        public Guid? SelectedUserId { get; set; }

        [BindProperty]
        public string FullName { get; set; } = string.Empty;

        [BindProperty]
        public string Dni { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Phone { get; set; } = string.Empty;

        [BindProperty]
        public string Role { get; set; } = "Médico";

        [BindProperty]
        public string Speciality { get; set; } = string.Empty;

        [BindProperty]
        public bool IsActive { get; set; } = true;

        [BindProperty]
        public string? TemporaryPassword { get; set; }

        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public List<SelectListItem> RoleOptions { get; set; } = new();
        public List<SelectListItem> SpecialityOptions { get; set; } = new();
        public List<UserRow> Users { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadRoleOptionsAsync();
            await LoadSpecialityOptionsAsync();
            await LoadUsersAsync();

            if (EditId.HasValue)
            {
                await LoadSelectedUserAsync(EditId.Value);
            }
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            await LoadRoleOptionsAsync();
            await LoadSpecialityOptionsAsync();

            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Role))
            {
                ErrorMessage = "Completa nombre, correo y rol.";
                await LoadUsersAsync();
                return Page();
            }

            if (Role.Equals("Paciente", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Este panel solo administra usuarios que no sean pacientes.";
                await LoadUsersAsync();
                return Page();
            }

            var roleName = NormalizeRole(Role);
            if (!AllowedRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
            {
                ErrorMessage = "Rol no permitido para este formulario.";
                await LoadUsersAsync();
                return Page();
            }

            var isMedicRole = roleName.Equals("Médico", StringComparison.OrdinalIgnoreCase) || roleName.Equals("Medico", StringComparison.OrdinalIgnoreCase);
            if (isMedicRole)
            {
                if (string.IsNullOrWhiteSpace(Speciality))
                {
                    ErrorMessage = "Para el rol médico debes seleccionar una especialidad activa.";
                    await LoadUsersAsync();
                    return Page();
                }

                var hasActiveSpeciality = await _db.Specialities.AsNoTracking()
                    .AnyAsync(s => s.IsActive && s.Name == Speciality.Trim());

                if (!hasActiveSpeciality)
                {
                    ErrorMessage = "La especialidad seleccionada no está activa.";
                    await LoadUsersAsync();
                    return Page();
                }
            }
            else
            {
                Speciality = string.Empty;
            }

            if (SelectedUserId.HasValue)
            {
                var user = await _userManager.FindByIdAsync(SelectedUserId.Value.ToString());
                if (user == null)
                {
                    ErrorMessage = "El usuario no existe.";
                    await LoadUsersAsync();
                    return Page();
                }

                user.FullName = FullName.Trim();
                user.Dni = Dni.Trim();
                user.Email = Email.Trim();
                user.UserName = Email.Trim();
                user.PhoneNumber = Phone.Trim();
                user.Speciality = Speciality.Trim();
                user.LockoutEnabled = !IsActive;
                user.LockoutEnd = IsActive ? null : DateTime.UtcNow.AddYears(100);

                var currentRoles = await _userManager.GetRolesAsync(user);
                var desiredRole = roleName;
                if (currentRoles.Count == 0 || !currentRoles.Contains(desiredRole))
                {
                    if (currentRoles.Count > 0)
                    {
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    }

                    await EnsureRoleExistsAsync(desiredRole);
                    await _userManager.AddToRoleAsync(user, desiredRole);
                }

                var update = await _userManager.UpdateAsync(user);
                if (!update.Succeeded)
                {
                    ErrorMessage = string.Join("; ", update.Errors.Select(e => e.Description));
                    await LoadUsersAsync();
                    return Page();
                }

                StatusMessage = "Usuario actualizado correctamente.";
                return RedirectToPage(new { editId = (Guid?)null });
            }

            var newUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = Email.Trim(),
                Email = Email.Trim(),
                EmailConfirmed = true,
                FullName = FullName.Trim(),
                Dni = Dni.Trim(),
                PhoneNumber = Phone.Trim(),
                Speciality = Speciality.Trim(),
                IsTemporaryPassword = true,
                FirstLoginCompleted = false,
                LockoutEnabled = !IsActive,
                LockoutEnd = IsActive ? null : DateTime.UtcNow.AddYears(100)
            };

            var tempPassword = string.IsNullOrWhiteSpace(TemporaryPassword)
                ? Guid.NewGuid().ToString("N")[..12]
                : TemporaryPassword;

            var create = await _userManager.CreateAsync(newUser, tempPassword);
            if (!create.Succeeded)
            {
                ErrorMessage = string.Join("; ", create.Errors.Select(e => e.Description));
                await LoadUsersAsync();
                return Page();
            }

            await EnsureRoleExistsAsync(roleName);
            await _userManager.AddToRoleAsync(newUser, roleName);

            StatusMessage = "Usuario creado correctamente.";
            return RedirectToPage(new { editId = (Guid?)null });
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                ErrorMessage = "El usuario no existe.";
                return RedirectToPage();
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any(r => r.Equals("Paciente", StringComparison.OrdinalIgnoreCase)))
            {
                ErrorMessage = "Este panel no elimina pacientes.";
                return RedirectToPage();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                ErrorMessage = string.Join("; ", result.Errors.Select(e => e.Description));
                return RedirectToPage();
            }

            StatusMessage = "Usuario eliminado correctamente.";
            return RedirectToPage();
        }

        private Task LoadRoleOptionsAsync()
        {
            Role = NormalizeRole(Role);
            if (!AllowedRoles.Contains(Role, StringComparer.OrdinalIgnoreCase))
            {
                Role = "Médico";
            }

            RoleOptions = AllowedRoles
                .Select(role => new SelectListItem(role, role, string.Equals(role, Role, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return Task.CompletedTask;
        }

        private async Task LoadSpecialityOptionsAsync()
        {
            SpecialityOptions = await _db.Specialities.AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem(s.Name, s.Name, s.Name == Speciality))
                .ToListAsync();
        }

        private async Task LoadUsersAsync()
        {
            var users = await _userManager.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync();
            var rows = new List<UserRow>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Any(r => r.Equals("Paciente", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var primaryRole = roles.FirstOrDefault() ?? "Sin rol";
                rows.Add(new UserRow
                {
                    Id = user.Id,
                    FullName = user.FullName ?? user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Phone = user.PhoneNumber ?? string.Empty,
                    PrimaryRole = primaryRole,
                    IsActive = user.LockoutEnabled == false || user.LockoutEnd == null || user.LockoutEnd <= DateTime.UtcNow
                });
            }

            Users = rows;
            TotalUsers = rows.Count;
            ActiveUsers = rows.Count(r => r.IsActive);
            InactiveUsers = rows.Count - ActiveUsers;
        }

        private async Task LoadSelectedUserAsync(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return;
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any(r => r.Equals("Paciente", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            SelectedUserId = user.Id;
            FullName = user.FullName ?? string.Empty;
            Dni = user.Dni ?? string.Empty;
            Email = user.Email ?? string.Empty;
            Phone = user.PhoneNumber ?? string.Empty;
            Speciality = user.Speciality ?? string.Empty;
            IsActive = user.LockoutEnabled == false || user.LockoutEnd == null || user.LockoutEnd <= DateTime.UtcNow;
            Role = NormalizeRole(roles.FirstOrDefault() ?? Role);
            if (!AllowedRoles.Contains(Role, StringComparer.OrdinalIgnoreCase))
            {
                Role = "Médico";
            }
        }

        private async Task EnsureRoleExistsAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        private static string NormalizeRole(string role)
        {
            return role switch
            {
                "Farmaceutico" => "Farmacéutico",
                "Medico" => "Médico",
                _ => role
            };
        }

        public class UserRow
        {
            public Guid Id { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string PrimaryRole { get; set; } = string.Empty;
            public bool IsActive { get; set; }
        }
    }
}
