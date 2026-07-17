using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Api.Pages.Pharmacy
{
    [Authorize(Roles = "Farmacéutico,Farmaceutico")]
    public class InventoryModel : PageModel
    {
        private readonly CaveroDbContext _db;

        public InventoryModel(CaveroDbContext db)
        {
            _db = db;
        }

        public List<InventoryItem> Items { get; set; } = new();

        public async Task OnGetAsync()
        {
            Items = await _db.InventoryItems.AsNoTracking()
                .OrderBy(i => i.Name)
                .Take(50)
                .ToListAsync();
        }
    }
}
