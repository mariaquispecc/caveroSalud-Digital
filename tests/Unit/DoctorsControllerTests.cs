using System;
using System.Security.Claims;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Features.Appointments.Controllers;
using CaveroSalud.Features.Doctors.Controllers;
using CaveroSalud.Features.Doctors.DTOs;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaveroSalud.Tests.Unit
{
    public class DoctorsControllerTests
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
        public async Task AddAvailability_ReturnsBadRequest_WhenOverlapExists()
        {
            using var db = CreateInMemoryContext();
            var doctorId = Guid.NewGuid();
            db.DoctorAvailabilities.Add(new DoctorAvailability
            {
                Id = Guid.NewGuid(),
                DoctorId = doctorId,
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(1).AddHours(2)
            });
            await db.SaveChangesAsync();

            var controller = new DoctorsController(db);
            SetUserPrincipal(controller, doctorId);

            var dto = new AvailabilityDto
            {
                StartAt = DateTime.UtcNow.AddDays(1).AddHours(1),
                EndAt = DateTime.UtcNow.AddDays(1).AddHours(3)
            };

            var result = await controller.AddAvailability(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddAvailability_ReturnsOk_WhenNoOverlap()
        {
            using var db = CreateInMemoryContext();
            var doctorId = Guid.NewGuid();
            var controller = new DoctorsController(db);
            SetUserPrincipal(controller, doctorId);

            var dto = new AvailabilityDto
            {
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(1).AddHours(2)
            };

            var result = await controller.AddAvailability(dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task CancelAppointment_ReturnsBadRequest_WhenWithin24Hours()
        {
            using var db = CreateInMemoryContext();
            var patientId = Guid.NewGuid();
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                DoctorId = Guid.NewGuid(),
                Speciality = "General",
                StartAt = DateTime.UtcNow.AddHours(10),
                EndAt = DateTime.UtcNow.AddHours(11),
                Status = AppointmentStatus.Scheduled
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();

            var controller = new AppointmentsController(db);
            SetUserPrincipal(controller, patientId);

            var result = await controller.Cancel(appointment.Id);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
