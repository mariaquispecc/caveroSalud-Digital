using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaveroSalud.Api.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [BindProperty]
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        public string? Email { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public void OnGet()
        {
            // ReturnUrl is bound automatically from query string.
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var email = (Email ?? string.Empty).Trim();
            var password = Password ?? string.Empty;

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
                return Page();
            }

            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, password, false);
            if (!signInResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
                return Page();
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

            if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return LocalRedirect(ReturnUrl);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.Count > 0 ? roles[0] : string.Empty;
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

            return LocalRedirect(redirect);
        }
    }
}
