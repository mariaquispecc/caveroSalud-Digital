using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Features.Doctors.DTOs;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CaveroSalud.Tests.Integration
{
    public class DoctorsIntegrationTests : IClassFixture<ApiTestFactory>
    {
        private readonly ApiTestFactory _factory;

        public DoctorsIntegrationTests(ApiTestFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task AddAvailability_ReturnsOk_WhenNoOverlap()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var doctorId = Guid.NewGuid();
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{doctorId}|Médico");

            var dto = new AvailabilityDto
            {
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(1).AddHours(2)
            };

            var response = await client.PostAsJsonAsync("/api/v1/doctor/availability", dto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var payload = await response.Content.ReadFromJsonAsync<object>();
            Assert.NotNull(payload);
        }

        [Fact]
        public async Task AddAvailability_ReturnsBadRequest_WhenOverlaps()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var doctorId = Guid.NewGuid();
            db.DoctorAvailabilities.Add(new DoctorAvailability
            {
                Id = Guid.NewGuid(),
                DoctorId = doctorId,
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(1).AddHours(2)
            });
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{doctorId}|Médico");

            var dto = new AvailabilityDto
            {
                StartAt = DateTime.UtcNow.AddDays(1).AddHours(1),
                EndAt = DateTime.UtcNow.AddDays(1).AddHours(3)
            };

            var response = await client.PostAsJsonAsync("/api/v1/doctor/availability", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AttendAppointment_CreatesClinicalRecordAndPrescriptions()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var doctorId = Guid.NewGuid();
            var patientId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();
            db.Appointments.Add(new Appointment
            {
                Id = appointmentId,
                DoctorId = doctorId,
                PatientId = patientId,
                Speciality = "General",
                StartAt = DateTime.UtcNow.AddHours(-1),
                EndAt = DateTime.UtcNow.AddHours(1),
                Status = AppointmentStatus.Scheduled
            });
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{doctorId}|Médico");

            var dto = new AttendAppointmentDto
            {
                Diagnosis = "Test diagnosis",
                Treatment = "Test treatment",
                LabOrders = new System.Collections.Generic.List<LabOrderDto>
                {
                    new LabOrderDto { TestName = "Blood Test" }
                },
                Prescriptions = new System.Collections.Generic.List<PrescriptionDto>
                {
                    new PrescriptionDto { Medication = "Aspirin", Dosage = "100mg", Quantity = 10 }
                }
            };

            var response = await client.PostAsJsonAsync($"/api/v1/doctor/attend/{appointmentId}", dto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var payload = await response.Content.ReadFromJsonAsync<object>();
            Assert.NotNull(payload);

            var record = await db.ClinicalRecords.FirstOrDefaultAsync(c => c.AppointmentId == appointmentId);
            Assert.NotNull(record);
            var prescription = await db.Prescriptions.FirstOrDefaultAsync(p => p.AppointmentId == appointmentId);
            Assert.NotNull(prescription);
            var labOrder = await db.LabOrders.FirstOrDefaultAsync(l => l.AppointmentId == appointmentId);
            Assert.NotNull(labOrder);
        }

        [Fact]
        public async Task RemoveAvailability_ReturnsOk_WhenDoctorOwnsAvailability()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var doctorId = Guid.NewGuid();
            var availabilityId = Guid.NewGuid();
            db.DoctorAvailabilities.Add(new DoctorAvailability
            {
                Id = availabilityId,
                DoctorId = doctorId,
                StartAt = DateTime.UtcNow.AddDays(2),
                EndAt = DateTime.UtcNow.AddDays(2).AddHours(2)
            });
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{doctorId}|Médico");

            var response = await client.DeleteAsync($"/api/v1/doctor/availability/{availabilityId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Dashboard_ReturnsCounts_WhenDoctorHasData()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var doctorId = Guid.NewGuid();
            var patientId = Guid.NewGuid();
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                DoctorId = doctorId,
                PatientId = patientId,
                Speciality = "General",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(1),
                Status = AppointmentStatus.Scheduled
            };
            db.Appointments.Add(appointment);
            db.ClinicalRecords.Add(new ClinicalRecord
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                DoctorId = doctorId,
                PatientId = patientId,
                Diagnosis = "Diag",
                Treatment = "Treat",
                Observations = "Obs",
                CreatedAt = DateTime.UtcNow,
                IsClosed = false
            });
            db.Prescriptions.Add(new Prescription
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                DoctorId = doctorId,
                PatientId = patientId,
                CreatedAt = DateTime.UtcNow,
                Status = PrescriptionStatuses.Requested,
                DeliveryNotes = string.Empty
            });
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{doctorId}|Médico");

            var response = await client.GetAsync("/api/v1/doctor/dashboard");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var dashboard = await response.Content.ReadFromJsonAsync<DashboardDto>();
            Assert.NotNull(dashboard);
            Assert.Equal(1, dashboard.PatientsSeenCount);
            Assert.Equal(1, dashboard.PrescriptionsIssuedCount);
            Assert.NotEmpty(dashboard.TodayAppointments);
        }

        [Fact]
        public async Task AddAvailability_ReturnsBadRequest_WhenEndBeforeStart()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var doctorId = Guid.NewGuid();
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{doctorId}|Médico");

            var dto = new AvailabilityDto
            {
                StartAt = DateTime.UtcNow.AddDays(1).AddHours(2),
                EndAt = DateTime.UtcNow.AddDays(1).AddHours(1)
            };

            var response = await client.PostAsJsonAsync("/api/v1/doctor/availability", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RemoveAvailability_ReturnsNotFound_WhenAvailabilityDoesNotExist()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Médico");

            var response = await client.DeleteAsync($"/api/v1/doctor/availability/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RemoveAvailability_ReturnsForbid_WhenDoctorDoesNotOwnAvailability()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var availability = new DoctorAvailability
            {
                Id = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                StartAt = DateTime.UtcNow.AddDays(2),
                EndAt = DateTime.UtcNow.AddDays(2).AddHours(1)
            };
            db.DoctorAvailabilities.Add(availability);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Médico");

            var response = await client.DeleteAsync($"/api/v1/doctor/availability/{availability.Id}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AttendAppointment_ReturnsNotFound_WhenAppointmentNotFound()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Médico");

            var dto = new AttendAppointmentDto
            {
                Diagnosis = "None",
                Treatment = "None"
            };

            var response = await client.PostAsJsonAsync($"/api/v1/doctor/attend/{Guid.NewGuid()}", dto);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AttendAppointment_ReturnsForbid_WhenDoctorDoesNotOwnAppointment()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Speciality = "General",
                StartAt = DateTime.UtcNow.AddHours(-1),
                EndAt = DateTime.UtcNow.AddHours(1),
                Status = AppointmentStatus.Scheduled
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Médico");

            var dto = new AttendAppointmentDto { Diagnosis = "Test", Treatment = "Test" };
            var response = await client.PostAsJsonAsync($"/api/v1/doctor/attend/{appointment.Id}", dto);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AttendAppointment_ReturnsBadRequest_WhenNotScheduled()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var doctorId = Guid.NewGuid();
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = doctorId,
                Speciality = "General",
                StartAt = DateTime.UtcNow.AddHours(-1),
                EndAt = DateTime.UtcNow.AddHours(1),
                Status = AppointmentStatus.Cancelled
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{doctorId}|Médico");

            var dto = new AttendAppointmentDto { Diagnosis = "Test", Treatment = "Test" };
            var response = await client.PostAsJsonAsync($"/api/v1/doctor/attend/{appointment.Id}", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
