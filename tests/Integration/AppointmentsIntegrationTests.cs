using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Features.Appointments.DTOs;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CaveroSalud.Tests.Integration
{
    public class AppointmentsIntegrationTests : IClassFixture<ApiTestFactory>
    {
        private readonly ApiTestFactory _factory;

        public AppointmentsIntegrationTests(ApiTestFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateAppointment_ReturnsCreated_WhenAppointmentIsValid()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var doctorId = Guid.NewGuid();
            var patientId = Guid.NewGuid();
            db.DoctorAvailabilities.Add(new DoctorAvailability
            {
                Id = Guid.NewGuid(),
                DoctorId = doctorId,
                StartAt = DateTime.UtcNow.AddDays(2),
                EndAt = DateTime.UtcNow.AddDays(2).AddHours(4)
            });
            await db.SaveChangesAsync();
            await SeedActiveSpecialityAsync(db, "Cardiology");

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{patientId}|Paciente");

            var dto = new CreateAppointmentDto
            {
                DoctorId = doctorId,
                Speciality = "Cardiology",
                StartAt = DateTime.UtcNow.AddDays(2).AddHours(1),
                EndAt = DateTime.UtcNow.AddDays(2).AddHours(2)
            };

            var response = await client.PostAsJsonAsync("/api/v1/appointments", dto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var appointment = await response.Content.ReadFromJsonAsync<object>();
            Assert.NotNull(appointment);
        }

        [Fact]
        public async Task CreateAppointment_ReturnsBadRequest_WhenSlotUnavailable()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var doctorId = Guid.NewGuid();
            var patientId = Guid.NewGuid();
            db.DoctorAvailabilities.Add(new DoctorAvailability
            {
                Id = Guid.NewGuid(),
                DoctorId = doctorId,
                StartAt = DateTime.UtcNow.AddDays(2).AddHours(1),
                EndAt = DateTime.UtcNow.AddDays(2).AddHours(2)
            });
            db.Appointments.Add(new Appointment
            {
                Id = Guid.NewGuid(),
                DoctorId = doctorId,
                PatientId = patientId,
                Speciality = "Cardiology",
                StartAt = DateTime.UtcNow.AddDays(2).AddHours(1),
                EndAt = DateTime.UtcNow.AddDays(2).AddHours(2),
                Status = AppointmentStatus.Scheduled
            });
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{patientId}|Paciente");

            var dto = new CreateAppointmentDto
            {
                DoctorId = doctorId,
                Speciality = "Cardiology",
                StartAt = DateTime.UtcNow.AddDays(2).AddHours(1),
                EndAt = DateTime.UtcNow.AddDays(2).AddHours(2)
            };

            var response = await client.PostAsJsonAsync("/api/v1/appointments", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task MyAppointments_ReturnsOwnScheduledAppointments()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var patientId = Guid.NewGuid();
            db.Appointments.Add(new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                DoctorId = Guid.NewGuid(),
                Speciality = "Dermatology",
                StartAt = DateTime.UtcNow.AddDays(3),
                EndAt = DateTime.UtcNow.AddDays(3).AddHours(1),
                Status = AppointmentStatus.Scheduled
            });
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{patientId}|Paciente");

            var resp = await client.GetAsync("/api/v1/appointments/mine");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var appointments = await resp.Content.ReadFromJsonAsync<AppointmentDto[]>();
            Assert.NotNull(appointments);
            Assert.Single(appointments);
        }

        [Fact]
        public async Task CancelAppointment_ReturnsOk_WhenMoreThan24HoursBefore()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var patientId = Guid.NewGuid();
            var appt = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                DoctorId = Guid.NewGuid(),
                Speciality = "Ophthalmology",
                StartAt = DateTime.UtcNow.AddDays(3),
                EndAt = DateTime.UtcNow.AddDays(3).AddHours(1),
                Status = AppointmentStatus.Scheduled
            };
            db.Appointments.Add(appt);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{patientId}|Paciente");

            var resp = await client.DeleteAsync($"/api/v1/appointments/{appt.Id}");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            await db.Entry(appt).ReloadAsync();
            var updated = await db.Appointments.FindAsync(appt.Id);
            Assert.Equal(AppointmentStatus.Cancelled, updated.Status);
        }

        [Fact]
        public async Task CreateAppointment_ReturnsBadRequest_WhenEndBeforeStart()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var doctorId = Guid.NewGuid();
            var patientId = Guid.NewGuid();
            db.DoctorAvailabilities.Add(new DoctorAvailability
            {
                Id = Guid.NewGuid(),
                DoctorId = doctorId,
                StartAt = DateTime.UtcNow.AddDays(2),
                EndAt = DateTime.UtcNow.AddDays(2).AddHours(4)
            });
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{patientId}|Paciente");

            var dto = new CreateAppointmentDto
            {
                DoctorId = doctorId,
                Speciality = "Cardiology",
                StartAt = DateTime.UtcNow.AddDays(2).AddHours(3),
                EndAt = DateTime.UtcNow.AddDays(2).AddHours(2)
            };

            var response = await client.PostAsJsonAsync("/api/v1/appointments", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateAppointment_ReturnsBadRequest_WhenCreatedInThePast()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var patientId = Guid.NewGuid();
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{patientId}|Paciente");

            var dto = new CreateAppointmentDto
            {
                DoctorId = Guid.NewGuid(),
                Speciality = "Dermatology",
                StartAt = DateTime.UtcNow.AddHours(-3),
                EndAt = DateTime.UtcNow.AddHours(-2)
            };

            var response = await client.PostAsJsonAsync("/api/v1/appointments", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CancelAppointment_ReturnsBadRequest_WhenWithin24Hours()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var patientId = Guid.NewGuid();
            var appt = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                DoctorId = Guid.NewGuid(),
                Speciality = "Ophthalmology",
                StartAt = DateTime.UtcNow.AddHours(23),
                EndAt = DateTime.UtcNow.AddHours(24),
                Status = AppointmentStatus.Scheduled
            };
            db.Appointments.Add(appt);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{patientId}|Paciente");

            var resp = await client.DeleteAsync($"/api/v1/appointments/{appt.Id}");
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task CancelAppointment_ReturnsForbid_WhenPatientDoesNotOwnAppointment()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var patientId = Guid.NewGuid();
            var appt = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Speciality = "Ophthalmology",
                StartAt = DateTime.UtcNow.AddDays(3),
                EndAt = DateTime.UtcNow.AddDays(3).AddHours(1),
                Status = AppointmentStatus.Scheduled
            };
            db.Appointments.Add(appt);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{patientId}|Paciente");

            var resp = await client.DeleteAsync($"/api/v1/appointments/{appt.Id}");
            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        private static async Task SeedActiveSpecialityAsync(CaveroDbContext db, string name)
        {
            if (!await db.Specialities.AnyAsync(s => s.Name == name))
            {
                db.Specialities.Add(new Speciality
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = "Test speciality",
                    IsActive = true
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
