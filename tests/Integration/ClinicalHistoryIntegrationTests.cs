using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CaveroSalud.Tests.Integration
{
    public class ClinicalHistoryIntegrationTests : IClassFixture<ApiTestFactory>
    {
        private readonly ApiTestFactory _factory;

        public ClinicalHistoryIntegrationTests(ApiTestFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task MyHistory_ReturnsOwnClinicalRecords_WhenPatientAuthenticated()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var patientId = Guid.NewGuid();
            db.ClinicalRecords.Add(new ClinicalRecord
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                DoctorId = Guid.NewGuid(),
                Diagnosis = "Test diagnosis",
                Treatment = "Test treatment",
                Observations = "Initial observation",
                CreatedAt = DateTime.UtcNow,
                IsClosed = false
            });
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{patientId}|Paciente");

            var response = await client.GetAsync("/api/v1/clinical/my-history");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var history = await response.Content.ReadFromJsonAsync<List<object>>();
            Assert.NotNull(history);
            Assert.Single(history);
        }

        [Fact]
        public async Task PatientHistory_ReturnsForbid_WhenDoctorHasNoContact()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var doctorId = Guid.NewGuid();
            var patientId = Guid.NewGuid();
            db.ClinicalRecords.Add(new ClinicalRecord
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                DoctorId = Guid.NewGuid(),
                Diagnosis = "Test diagnosis",
                Treatment = "Test treatment",
                Observations = "Observation",
                CreatedAt = DateTime.UtcNow,
                IsClosed = false
            });
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{doctorId}|Médico");

            var response = await client.GetAsync($"/api/v1/clinical/patient/{patientId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PatientHistory_ReturnsRecords_WhenDoctorHasContact()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var doctorId = Guid.NewGuid();
            var patientId = Guid.NewGuid();
            db.Appointments.Add(new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                DoctorId = doctorId,
                Speciality = "Cardiology",
                StartAt = DateTime.UtcNow.AddDays(-1),
                EndAt = DateTime.UtcNow.AddDays(-1).AddHours(1),
                Status = AppointmentStatus.Scheduled
            });
            db.ClinicalRecords.Add(new ClinicalRecord
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                DoctorId = doctorId,
                Diagnosis = "Test diagnosis",
                Treatment = "Test treatment",
                Observations = "Observation",
                CreatedAt = DateTime.UtcNow,
                IsClosed = false
            });
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{doctorId}|Médico");

            var response = await client.GetAsync($"/api/v1/clinical/patient/{patientId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var history = await response.Content.ReadFromJsonAsync<List<object>>();
            Assert.NotNull(history);
            Assert.Single(history);
        }

        [Fact]
        public async Task AddObservation_ReturnsOk_WhenDoctorOwnsOpenRecord()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var doctorId = Guid.NewGuid();
            var record = new ClinicalRecord
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = doctorId,
                Diagnosis = "Test",
                Treatment = "Treatment",
                Observations = "Initial",
                CreatedAt = DateTime.UtcNow,
                IsClosed = false
            };
            db.ClinicalRecords.Add(record);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{doctorId}|Médico");

            var dto = new { Observations = "Added note" };
            var resp = await client.PostAsJsonAsync($"/api/v1/clinical/records/{record.Id}/observations", dto);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            await db.Entry(record).ReloadAsync();
            var updated = await db.ClinicalRecords.FindAsync(record.Id);
            Assert.Contains("Added note", updated.Observations);
        }

        [Fact]
        public async Task CloseRecord_ReturnsOk_WhenDoctorOwnsRecord()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var doctorId = Guid.NewGuid();
            var record = new ClinicalRecord
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = doctorId,
                Diagnosis = "Test",
                Treatment = "Treatment",
                Observations = "Observation",
                CreatedAt = DateTime.UtcNow,
                IsClosed = false
            };
            db.ClinicalRecords.Add(record);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{doctorId}|Médico");

            var resp = await client.PostAsync($"/api/v1/clinical/records/{record.Id}/close", null);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            await db.Entry(record).ReloadAsync();
            var updated = await db.ClinicalRecords.FindAsync(record.Id);
            Assert.True(updated.IsClosed);
        }

        [Fact]
        public async Task AddObservation_ReturnsNotFound_WhenRecordMissing()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Médico");

            var dto = new { Observations = "Note" };
            var resp = await client.PostAsJsonAsync($"/api/v1/clinical/records/{Guid.NewGuid()}/observations", dto);
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task AddObservation_ReturnsForbid_WhenDoctorDoesNotOwnRecord()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var record = new ClinicalRecord
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Diagnosis = "Test",
                Treatment = "Treatment",
                Observations = "Initial",
                CreatedAt = DateTime.UtcNow,
                IsClosed = false
            };
            db.ClinicalRecords.Add(record);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Médico");

            var dto = new { Observations = "Added note" };
            var resp = await client.PostAsJsonAsync($"/api/v1/clinical/records/{record.Id}/observations", dto);
            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task AddObservation_ReturnsBadRequest_WhenRecordIsClosed()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var doctorId = Guid.NewGuid();
            var record = new ClinicalRecord
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = doctorId,
                Diagnosis = "Test",
                Treatment = "Treatment",
                Observations = "Initial",
                CreatedAt = DateTime.UtcNow,
                IsClosed = true
            };
            db.ClinicalRecords.Add(record);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{doctorId}|Médico");

            var dto = new { Observations = "Added note" };
            var resp = await client.PostAsJsonAsync($"/api/v1/clinical/records/{record.Id}/observations", dto);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task CloseRecord_ReturnsNotFound_WhenRecordMissing()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Médico");

            var resp = await client.PostAsync($"/api/v1/clinical/records/{Guid.NewGuid()}/close", null);
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task CloseRecord_ReturnsForbid_WhenDoctorDoesNotOwnRecord()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var record = new ClinicalRecord
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Diagnosis = "Test",
                Treatment = "Treatment",
                Observations = "Observation",
                CreatedAt = DateTime.UtcNow,
                IsClosed = false
            };
            db.ClinicalRecords.Add(record);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Médico");

            var resp = await client.PostAsync($"/api/v1/clinical/records/{record.Id}/close", null);
            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }
    }
}
