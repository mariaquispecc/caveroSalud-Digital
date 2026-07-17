using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Api.Pages.App
{
    [Authorize(Roles = "Administrador,Admin")]
    public class AdminSettingsModel : PageModel
    {
        private readonly CaveroDbContext _db;

        public AdminSettingsModel(CaveroDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        [Required]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        public string TagLine { get; set; } = string.Empty;

        [BindProperty]
        public string Description { get; set; } = string.Empty;

        [BindProperty]
        public string Address { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Phone { get; set; } = string.Empty;

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            var latest = await _db.PublicInfos.AsNoTracking()
                .OrderByDescending(p => p.UpdatedAt)
                .FirstOrDefaultAsync();

            Title = latest?.Title ?? "Clinica Cavero";
            TagLine = latest?.TagLine ?? "Salud y confianza";
            Description = latest?.Description ?? string.Empty;
            Address = latest?.Address ?? string.Empty;
            Email = latest?.Email ?? string.Empty;
            Phone = latest?.Phone ?? string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            var info = await _db.PublicInfos.OrderByDescending(p => p.UpdatedAt).FirstOrDefaultAsync();
            if (info == null)
            {
                info = new PublicInfo { Id = Guid.NewGuid() };
                _db.PublicInfos.Add(info);
            }

            info.Title = Title.Trim();
            info.TagLine = TagLine.Trim();
            info.Description = Description.Trim();
            info.Address = Address.Trim();
            info.Email = Email.Trim();
            info.Phone = Phone.Trim();
            info.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            StatusMessage = "Contenido del sitio actualizado correctamente.";
            return RedirectToPage();
        }
    }
}
