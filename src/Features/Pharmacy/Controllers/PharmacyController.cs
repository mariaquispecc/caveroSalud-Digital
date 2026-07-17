using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Features.Pharmacy.DTOs;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Features.Pharmacy.Controllers
{
    [ApiController]
    [Route("api/v1/pharmacy")]
    [Authorize(Roles = "Farmacéutico")]
    public class PharmacyController : ControllerBase
    {
        private readonly CaveroDbContext _db;

        public PharmacyController(CaveroDbContext db)
        {
            _db = db;
        }

        [HttpGet("prescriptions/pending")]
        public async Task<IActionResult> GetPendingPrescriptions()
        {
            var prescriptions = await _db.Prescriptions
                .Where(p => p.Status == PrescriptionStatuses.Requested)
                .OrderBy(p => p.CreatedAt)
                .Select(p => new PendingPrescriptionDto
                {
                    Id = p.Id,
                    AppointmentId = p.AppointmentId,
                    PatientId = p.PatientId,
                    DoctorId = p.DoctorId,
                    CreatedAt = p.CreatedAt,
                    Status = p.Status,
                    Items = p.Items.Select(i => new PrescriptionItemDto
                    {
                        Medication = i.Medication,
                        Dosage = i.Dosage,
                        Quantity = i.Quantity
                    }).ToList(),
                    PatientName = _db.Users.Where(u => u.Id == p.PatientId).Select(u => u.FullName).FirstOrDefault(),
                    PatientDni = _db.Users.Where(u => u.Id == p.PatientId).Select(u => u.Dni).FirstOrDefault()
                })
                .ToListAsync();

            return Ok(prescriptions);
        }

        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventoryAvailability()
        {
            var items = await _db.InventoryItems
                .Select(i => new InventoryAvailabilityDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Unit = i.Unit,
                    Quantity = i.Quantity,
                    MinThreshold = i.MinThreshold,
                    IsLowStock = i.Quantity <= i.MinThreshold
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("prescriptions/{prescriptionId}/deliver")]
        public async Task<IActionResult> DeliverPrescription(Guid prescriptionId, [FromBody] DeliverPrescriptionDto dto)
        {
            var prescription = await _db.Prescriptions
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == prescriptionId);

            if (prescription == null)
            {
                return NotFound();
            }

            if (prescription.Status != PrescriptionStatuses.Requested)
            {
                return BadRequest("Prescription is not pending delivery.");
            }

            foreach (var item in prescription.Items)
            {
                var inventoryItem = await _db.InventoryItems.FirstOrDefaultAsync(i => i.Name == item.Medication);
                if (inventoryItem == null)
                {
                    return BadRequest($"Inventory item not found for medication '{item.Medication}'.");
                }

                if (inventoryItem.Quantity < item.Quantity)
                {
                    return BadRequest($"Insufficient stock for '{item.Medication}'. Available: {inventoryItem.Quantity}, requested: {item.Quantity}.");
                }

                inventoryItem.Quantity -= item.Quantity;
            }

            prescription.Status = PrescriptionStatuses.Delivered;
            prescription.DeliveredById = GetUserId();
            prescription.DeliveredAt = DateTime.UtcNow;
            prescription.DeliveryNotes = dto?.DeliveryNotes;

            await _db.SaveChangesAsync();
            return Ok(new { message = "Prescription delivered successfully." });
        }

        [HttpGet("prescriptions/search")]
        public async Task<IActionResult> SearchPrescriptions([FromQuery] string dni, [FromQuery] string consultationNumber)
        {
            if (string.IsNullOrWhiteSpace(dni) && string.IsNullOrWhiteSpace(consultationNumber))
            {
                return BadRequest("Search requires patient DNI or consultation number.");
            }

            Guid? appointmentId = null;
            if (!string.IsNullOrWhiteSpace(consultationNumber))
            {
                if (Guid.TryParse(consultationNumber, out var parsedId))
                {
                    appointmentId = parsedId;
                }
                else
                {
                    return BadRequest("Consultation number must be a valid appointment GUID.");
                }
            }

            var query = _db.Prescriptions.AsQueryable();
            if (!string.IsNullOrWhiteSpace(dni))
            {
                query = query.Where(p => _db.Users.Any(u => u.Id == p.PatientId && u.Dni == dni));
            }

            if (appointmentId.HasValue)
            {
                query = query.Where(p => p.AppointmentId == appointmentId.Value);
            }

            var results = await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PendingPrescriptionDto
                {
                    Id = p.Id,
                    AppointmentId = p.AppointmentId,
                    PatientId = p.PatientId,
                    DoctorId = p.DoctorId,
                    CreatedAt = p.CreatedAt,
                    Status = p.Status,
                    Items = p.Items.Select(i => new PrescriptionItemDto
                    {
                        Medication = i.Medication,
                        Dosage = i.Dosage,
                        Quantity = i.Quantity
                    }).ToList(),
                    PatientName = _db.Users.Where(u => u.Id == p.PatientId).Select(u => u.FullName).FirstOrDefault(),
                    PatientDni = _db.Users.Where(u => u.Id == p.PatientId).Select(u => u.Dni).FirstOrDefault()
                })
                .ToListAsync();

            return Ok(results);
        }

        private Guid GetUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Guid.Parse(sub);
        }
    }
}
