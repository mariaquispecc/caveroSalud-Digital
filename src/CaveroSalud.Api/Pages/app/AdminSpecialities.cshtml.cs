using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
    public class AdminSpecialitiesModel : PageModel
    {
        private readonly CaveroDbContext _db;

        public AdminSpecialitiesModel(CaveroDbContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public Guid? EditId { get; set; }

        [BindProperty]
        [Required]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        public string Description { get; set; } = string.Empty;

        [BindProperty]
        public bool IsActive { get; set; } = true;

        public List<SpecialityRow> Specialities { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadSpecialitiesAsync();

            if (EditId.HasValue)
            {
                var speciality = await _db.Specialities.AsNoTracking().FirstOrDefaultAsync(s => s.Id == EditId.Value);
                if (speciality != null)
                {
                    Name = speciality.Name;
                    Description = speciality.Description;
                    IsActive = speciality.IsActive;
                }
            }
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadSpecialitiesAsync();
                return Page();
            }

            var trimmedName = Name.Trim();
            var duplicateExists = await _db.Specialities
                .AnyAsync(s => s.Id != (EditId ?? Guid.Empty) && s.Name.ToLower() == trimmedName.ToLower());

            if (duplicateExists)
            {
                ErrorMessage = "Ya existe una especialidad con ese nombre.";
                await LoadSpecialitiesAsync();
                return Page();
            }

            if (EditId.HasValue)
            {
                var speciality = await _db.Specialities.FirstOrDefaultAsync(s => s.Id == EditId.Value);
                if (speciality == null)
                {
                    ErrorMessage = "La especialidad no existe.";
                    return RedirectToPage();
                }

                speciality.Name = trimmedName;
                speciality.Description = Description.Trim();
                speciality.IsActive = IsActive;
                StatusMessage = "Especialidad actualizada.";
            }
            else
            {
                await _db.Specialities.AddAsync(new Speciality
                {
                    Id = Guid.NewGuid(),
                    Name = trimmedName,
                    Description = Description.Trim(),
                    IsActive = IsActive
                });
                StatusMessage = "Especialidad creada.";
            }

            await _db.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var speciality = await _db.Specialities.FirstOrDefaultAsync(s => s.Id == id);
            if (speciality == null)
            {
                ErrorMessage = "La especialidad no existe.";
                return RedirectToPage();
            }

            var hasAppointments = await _db.Appointments.AnyAsync(a => a.Speciality == speciality.Name);
            if (hasAppointments)
            {
                ErrorMessage = "No se puede eliminar porque existen citas asociadas. Puedes desactivarla.";
                return RedirectToPage();
            }

            _db.Specialities.Remove(speciality);
            await _db.SaveChangesAsync();
            StatusMessage = "Especialidad eliminada.";
            return RedirectToPage();
        }

        private async Task LoadSpecialitiesAsync()
        {
            Specialities = await _db.Specialities.AsNoTracking()
                .OrderBy(s => s.Name)
                .Select(s => new SpecialityRow
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = string.IsNullOrWhiteSpace(s.Description) ? "Sin descripción" : s.Description,
                    IsActive = s.IsActive
                })
                .ToListAsync();
        }

        public class SpecialityRow
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public bool IsActive { get; set; }
        }
    }
}
