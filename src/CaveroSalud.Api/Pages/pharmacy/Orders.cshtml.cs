using System;
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
    public class OrdersModel : PageModel
    {
        private readonly CaveroDbContext _db;

        public OrdersModel(CaveroDbContext db)
        {
            _db = db;
        }

        public List<OrderRow> Prescriptions { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userNames = await _db.Users.AsNoTracking()
                .Select(u => new { u.Id, Name = u.FullName ?? u.UserName ?? string.Empty })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            Prescriptions = await _db.Prescriptions.AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .Select(p => new OrderRow
                {
                    ShortId = p.Id.ToString().Substring(0, 8),
                    PatientId = p.PatientId,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            foreach (var item in Prescriptions)
            {
                item.PatientName = userNames.TryGetValue(item.PatientId, out var name) ? name : item.PatientId.ToString();
            }
        }

        public class OrderRow
        {
            public string ShortId { get; set; } = string.Empty;
            public Guid PatientId { get; set; }
            public string PatientName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }
    }
}
