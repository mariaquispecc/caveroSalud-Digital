using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Features.Laboratory.DTOs;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Features.Laboratory.Controllers
{
    [ApiController]
    [Route("api/v1/lab")]
    [Authorize(Roles = "Laboratorista")]
    public class LaboratoryController : ControllerBase
    {
        private readonly CaveroDbContext _db;

        public LaboratoryController(CaveroDbContext db)
        {
            _db = db;
        }

        [HttpGet("orders/pending")]
        public async Task<IActionResult> PendingOrders()
        {
            var orders = await _db.LabOrders
                .Where(o => o.Status == "Requested")
                .OrderBy(o => o.CreatedAt)
                .Select(o => new LabOrderDto { Id = o.Id, AppointmentId = o.AppointmentId, PatientId = o.PatientId, TestName = o.TestName, Status = o.Status, CreatedAt = o.CreatedAt })
                .ToListAsync();
            return Ok(orders);
        }

        [HttpPost("orders/{id}/results")]
        public async Task<IActionResult> SubmitResults(Guid id, [FromBody] SubmitResultsDto dto)
        {
            var order = await _db.LabOrders.FindAsync(id);
            if (order == null) return NotFound();
            if (order.Status != "Requested") return BadRequest("Order not in requested state");

            var labResults = dto.Results.Select(r => new LabResult
            {
                Id = Guid.NewGuid(),
                LabOrderId = order.Id,
                Analyte = r.Analyte,
                Value = r.Value,
                Unit = r.Unit,
                ReferenceRange = r.ReferenceRange,
                Comments = r.Comments,
                CreatedAt = DateTime.UtcNow,
                Published = false
            }).ToList();

            await _db.LabResults.AddRangeAsync(labResults);
            order.Status = "Reported";
            await _db.SaveChangesAsync();
            return Ok(new { message = "Results submitted" });
        }

        [HttpPost("orders/{id}/validate")]
        public async Task<IActionResult> ValidateReport(Guid id)
        {
            var order = await _db.LabOrders.FindAsync(id);
            if (order == null) return NotFound();
            var results = await _db.LabResults.Where(r => r.LabOrderId == id).ToListAsync();
            if (!results.Any()) return BadRequest("No results to validate");

            var laboratorianId = GetUserId();
            foreach (var r in results)
            {
                r.ValidatedBy = laboratorianId;
                r.ValidatedAt = DateTime.UtcNow;
                r.Published = true;
            }

            order.Status = "Validated";
            await _db.SaveChangesAsync();

            // Visibility: once validated/published, it will be visible to doctor and patient via their endpoints
            return Ok(new { message = "Report validated and published" });
        }

        [HttpGet("inventory")]
        public async Task<IActionResult> Inventory()
        {
            var items = await _db.InventoryItems.Select(i => new InventoryDto { Id = i.Id, Name = i.Name, Unit = i.Unit, Quantity = i.Quantity, MinThreshold = i.MinThreshold }).ToListAsync();
            return Ok(items);
        }

        [HttpPost("inventory")]
        public async Task<IActionResult> UpsertInventory([FromBody] UpsertInventoryDto dto)
        {
            // If exists by name, update
            var existing = await _db.InventoryItems.FirstOrDefaultAsync(i => i.Name == dto.Name);
            if (existing != null)
            {
                existing.Quantity = dto.Quantity;
                existing.MinThreshold = dto.MinThreshold;
                existing.Unit = dto.Unit;
            }
            else
            {
                var item = new InventoryItem { Id = Guid.NewGuid(), Name = dto.Name, Unit = dto.Unit, Quantity = dto.Quantity, MinThreshold = dto.MinThreshold };
                await _db.InventoryItems.AddAsync(item);
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("inventory/{id}")]
        public async Task<IActionResult> UpdateInventory(Guid id, [FromBody] UpsertInventoryDto dto)
        {
            var item = await _db.InventoryItems.FindAsync(id);
            if (item == null) return NotFound();
            item.Name = dto.Name;
            item.Unit = dto.Unit;
            item.Quantity = dto.Quantity;
            item.MinThreshold = dto.MinThreshold;
            await _db.SaveChangesAsync();
            return Ok();
        }

        private Guid GetUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Guid.Parse(sub);
        }
    }
}
