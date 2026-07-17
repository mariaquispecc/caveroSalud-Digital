using System.Collections.Generic;
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
    [Authorize(Roles = "Farmacéutico,Farmaceutico")]
    public class FarmaciaInventarioModel : PageModel
    {
        private readonly CaveroDbContext _db;

        public FarmaciaInventarioModel(CaveroDbContext db)
        {
            _db = db;
        }

        public List<InventoryItem> Items { get; set; } = new();

        [BindProperty]
        public InventoryInput Input { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadItemsAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                ErrorMessage = "El nombre del medicamento es obligatorio.";
                await LoadItemsAsync();
                return Page();
            }

            if (Input.MinThreshold < 0 || Input.Quantity < 0)
            {
                ErrorMessage = "Cantidad y stock mínimo no pueden ser negativos.";
                await LoadItemsAsync();
                return Page();
            }

            var normalizedName = Input.Name.Trim();
            var existing = await _db.InventoryItems.FirstOrDefaultAsync(i => i.Name == normalizedName);
            if (existing == null)
            {
                await _db.InventoryItems.AddAsync(new InventoryItem
                {
                    Id = Guid.NewGuid(),
                    Name = normalizedName,
                    Unit = string.IsNullOrWhiteSpace(Input.Unit) ? "und" : Input.Unit.Trim(),
                    Quantity = Input.Quantity,
                    MinThreshold = Input.MinThreshold
                });
                StatusMessage = "Medicamento agregado al inventario.";
            }
            else
            {
                existing.Unit = string.IsNullOrWhiteSpace(Input.Unit) ? existing.Unit : Input.Unit.Trim();
                existing.Quantity = Input.Quantity;
                existing.MinThreshold = Input.MinThreshold;
                StatusMessage = "Medicamento actualizado en inventario.";
            }

            await _db.SaveChangesAsync();
            return RedirectToPage();
        }

        private async Task LoadItemsAsync()
        {
            Items = await _db.InventoryItems.AsNoTracking()
                .OrderBy(i => i.Name)
                .Take(100)
                .ToListAsync();
        }

        public class InventoryInput
        {
            public string Name { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public decimal Quantity { get; set; }
            public decimal MinThreshold { get; set; }
        }
    }
}
