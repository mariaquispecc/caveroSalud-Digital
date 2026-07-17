using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Api.Pages
{
    public class IndexModel : PageModel
    {
        private readonly CaveroDbContext _db;

        public IndexModel(CaveroDbContext db)
        {
            _db = db;
        }

        public PublicInfo SiteInfo { get; set; } = new PublicInfo
        {
            Title = "Atención de salud cercana y digital",
            TagLine = "Gestión de citas, resultados de laboratorio y recetas desde tu dispositivo.",
            Description = "Servicio seguro y accesible para la comunidad.",
            Address = string.Empty,
            Email = string.Empty,
            Phone = string.Empty
        };

        public List<HomeSpecialityCard> Specialities { get; set; } = new();
        public string PanelUrl { get; set; } = "/login";
        public List<HomeSectionCard> Sections { get; set; } = new();

        public async Task OnGetAsync()
        {
            var latestInfo = await _db.PublicInfos.AsNoTracking()
                .OrderByDescending(p => p.UpdatedAt)
                .FirstOrDefaultAsync();

            if (latestInfo != null)
            {
                SiteInfo = latestInfo;
            }

            Specialities = await _db.Specialities.AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new HomeSpecialityCard
                {
                    Name = s.Name,
                    Description = string.IsNullOrWhiteSpace(s.Description)
                        ? "Especialidad registrada en el sistema"
                        : s.Description
                })
                .ToListAsync();

            if (User.Identity?.IsAuthenticated == true)
            {
                PanelUrl = await ResolvePanelUrlAsync();
            }

            Sections = new List<HomeSectionCard>
            {
                new HomeSectionCard
                {
                    Id = "pacientes",
                    Icon = "fa-user-injured",
                    Title = "Área de Pacientes",
                    Summary = "Agenda citas, revisa recordatorios y consulta tu historial clínico.",
                    DetailUrl = "/app/paciente"
                },
                new HomeSectionCard
                {
                    Id = "medicos",
                    Icon = "fa-user-doctor",
                    Title = "Área Médica",
                    Summary = "Gestiona agenda completa, registra atenciones y consulta historia clínica.",
                    DetailUrl = "/app/medico"
                },
                new HomeSectionCard
                {
                    Id = "laboratorio",
                    Icon = "fa-flask-vial",
                    Title = "Área de Laboratorio",
                    Summary = "Procesa órdenes, registra resultados, valida exámenes e inventario.",
                    DetailUrl = "/app/laboratorio"
                },
                new HomeSectionCard
                {
                    Id = "farmacia",
                    Icon = "fa-capsules",
                    Title = "Área de Farmacia",
                    Summary = "Despacha recetas y controla stock con alertas de mínimos.",
                    DetailUrl = "/app/farmacia"
                },
                new HomeSectionCard
                {
                    Id = "admin",
                    Icon = "fa-user-shield",
                    Title = "Área Administrativa",
                    Summary = "Administra usuarios, especialidades, configuración y mensajería interna.",
                    DetailUrl = "/app/admin"
                }
            };
        }

        private Task<string> ResolvePanelUrlAsync()
        {
            if (User.IsInRole("Administrador") || User.IsInRole("Admin"))
            {
                return Task.FromResult("/app/admin");
            }

            if (User.IsInRole("Paciente"))
            {
                return Task.FromResult("/app/paciente");
            }

            if (User.IsInRole("Médico") || User.IsInRole("Medico"))
            {
                return Task.FromResult("/app/medico");
            }

            if (User.IsInRole("Laboratorista"))
            {
                return Task.FromResult("/app/laboratorio");
            }

            if (User.IsInRole("Farmacéutico") || User.IsInRole("Farmaceutico"))
            {
                return Task.FromResult("/app/farmacia");
            }

            return Task.FromResult("/login");
        }

        public class HomeSpecialityCard
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        public class HomeSectionCard
        {
            public string Id { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Summary { get; set; } = string.Empty;
            public string DetailUrl { get; set; } = string.Empty;
        }
    }
}