using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Api.Pages
{
    public class SpecialtiesModel : PageModel
    {
        private readonly CaveroDbContext _db;

        public SpecialtiesModel(CaveroDbContext db)
        {
            _db = db;
        }

        public List<SpecialityCard> Specialities { get; set; } = new();

        public async Task OnGetAsync()
        {
            Specialities = await _db.Specialities.AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new SpecialityCard
                {
                    Name = s.Name,
                    Description = string.IsNullOrWhiteSpace(s.Description)
                        ? "Especialidad activa en Clinica Cavero"
                        : s.Description
                })
                .ToListAsync();
        }

        public class SpecialityCard
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }
    }
}
