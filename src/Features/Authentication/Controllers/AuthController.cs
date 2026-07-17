using System;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Features.Authentication.DTOs;
using CaveroSalud.Infrastructure.Identity;
using CaveroSalud.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Features.Authentication.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly CaveroDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public AuthController(
            CaveroDbContext db,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _roleManager = roleManager;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterPatientDto dto)
        {
            // Patients self-register
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null) return BadRequest("Email already registrered");

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                Dni = dto.Dni,
                PhoneNumber = dto.Phone,
                Speciality = string.Empty
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            // Assign role 'Paciente'
            if (!await _roleManager.RoleExistsAsync("Paciente"))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>("Paciente"));
            }
            await _userManager.AddToRoleAsync(user, "Paciente");

            // Optionally send confirmation email (implement email tokens later)

            return Ok(new { message = "Registered" });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return Unauthorized();

            var signIn = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!signIn.Succeeded) return Unauthorized();

            await _signInManager.SignInAsync(user, false);

            // Determine redirect URL by role
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.Count > 0 ? roles[0] : "";
            var redirect = role switch
            {
                "Paciente" => "/app/paciente",
                "Médico" => "/app/medico",
                "Medico" => "/app/medico",
                "Laboratorista" => "/app/laboratorio",
                "Farmacéutico" => "/app/farmacia",
                "Farmaceutico" => "/app/farmacia",
                "Administrador" => "/app/admin",
                "Admin" => "/app/admin",
                _ => "/"
            };

            return Ok(new { email = user.Email, role, redirect, isTemporaryPassword = user.IsTemporaryPassword, firstLoginCompleted = user.FirstLoginCompleted });
        }

        [HttpPost("admin/create-user")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AdminCreateUser([FromBody] AdminCreateUserDto dto)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null) return BadRequest("Email already registrered");

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
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                Dni = dto.Dni,
                PhoneNumber = dto.Phone,
                Speciality = dto.Speciality,
                IsTemporaryPassword = true,
                FirstLoginCompleted = false
            };

            var tempPassword = dto.TemporaryPassword ?? Guid.NewGuid().ToString("N").Substring(0, 12);
            var result = await _userManager.CreateAsync(user, tempPassword);
            if (!result.Succeeded) return BadRequest(result.Errors);

            // Ensure role exists and assign
            if (!await _roleManager.RoleExistsAsync(normalizedRole))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(normalizedRole));
            }
            await _userManager.AddToRoleAsync(user, normalizedRole);

            // Generate password reset token so the user can set a new password securely
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = $"https://your-frontend.example/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";

            // Send email with temp credentials and reset link guidance
            var emailHtml = $"<p>Sus credenciales temporales: <b>{tempPassword}</b></p><p>Por seguridad, puede cambiar su contraseña desde este enlace: <a href=\"{resetUrl}\">Restablecer contraseña</a></p>";
            await _emailSender.SendEmailAsync(dto.Email, "Credenciales CaveroSalud", emailHtml);

            return Ok(new { message = "User created" });
        }

        [HttpPost("request-password-reset")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return Ok(); // Do not reveal existence

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = $"https://your-frontend.example/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";

            var html = $"<p>Para restablecer tu contraseña, haz click <a href=\"{resetUrl}\">aquí</a>.</p>";
            await _emailSender.SendEmailAsync(user.Email, "Restablecer contraseña - CaveroSalud", html);

            return Ok();
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return BadRequest("Invalid request");

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (!result.Succeeded) return BadRequest(result.Errors);

            // Clear temporary flags if any
            user.IsTemporaryPassword = false;
            user.FirstLoginCompleted = true;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Password reset" });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return BadRequest("Invalid request");

            var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);
            if (!result.Succeeded) return BadRequest(result.Errors);

            user.IsTemporaryPassword = false;
            user.FirstLoginCompleted = true;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Password changed" });
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
