using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Features.Public.DTOs;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaveroSalud.Features.Public.Controllers
{
    [ApiController]
    [Route("api/v1/public")]
    public class PublicController : ControllerBase
    {
        private readonly CaveroDbContext _db;

        public PublicController(CaveroDbContext db)
        {
            _db = db;
        }

        [HttpPost("contact")]
        [AllowAnonymous]
        public async Task<IActionResult> Contact([FromBody] ContactDto dto)
        {
            var msg = new ContactMessage
            {
                Id = System.Guid.NewGuid(),
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                Message = dto.Message,
                CreatedAt = System.DateTime.UtcNow
            };

            await _db.ContactMessages.AddAsync(msg);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Received" });
        }
    }
}
