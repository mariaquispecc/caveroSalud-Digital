using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaveroSalud.Api.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        public string FullName { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        public string Email { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [BindProperty]
        [Phone(ErrorMessage = "El teléfono no es válido.")]
        public string Phone { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existingUser = await _userManager.FindByEmailAsync(Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "El correo ya está registrado.");
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = Email,
                Email = Email,
                FullName = FullName,
                PhoneNumber = Phone,
                Dni = string.Empty,
                Speciality = string.Empty,
                IsTemporaryPassword = false,
                FirstLoginCompleted = true
            };

            var result = await _userManager.CreateAsync(user, Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            if (!await _roleManager.RoleExistsAsync("Paciente"))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>("Paciente"));
            }

            await _userManager.AddToRoleAsync(user, "Paciente");
            await _signInManager.SignInAsync(user, isPersistent: false);

            return LocalRedirect("/app/paciente");
        }
    }
}
