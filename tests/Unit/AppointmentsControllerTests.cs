using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Infrastructure.Identity;
using CaveroSalud.Features.Appointments.Controllers;
using CaveroSalud.Features.Appointments.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaveroSalud.Tests.Unit
{
    public class AppointmentsControllerTests
    {
        private static CaveroDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<CaveroDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new CaveroDbContext(options);
        }

        private static void SetUserPrincipal(ControllerBase controller, Guid userId)
        {
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    }, "TestAuth"))
                }
            };
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenEndBeforeStart()
        {
            using var db = CreateInMemoryContext();
            var controller = new AppointmentsController(db);
            SetUserPrincipal(controller, Guid.NewGuid());

            var dto = new CreateAppointmentDto
            {
                Speciality = "Cardiology",
                DoctorId = Guid.NewGuid(),
                StartAt = DateTime.UtcNow.AddDays(2),
                EndAt = DateTime.UtcNow.AddDays(1)
            };

            var result = await controller.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenSlotNotAvailable()
        {
            using var db = CreateInMemoryContext();
            var doctorId = Guid.NewGuid();
            db.DoctorAvailabilities.Add(new DoctorAvailability
            {
                Id = Guid.NewGuid(),
                DoctorId = doctorId,
                StartAt = DateTime.UtcNow.AddDays(3),
                EndAt = DateTime.UtcNow.AddDays(4)
            });
            await db.SaveChangesAsync();

            var controller = new AppointmentsController(db);
            SetUserPrincipal(controller, Guid.NewGuid());

            var dto = new CreateAppointmentDto
            {
                Speciality = "Cardiology",
                DoctorId = doctorId,
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(1).AddHours(1)
            };

            var result = await controller.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
