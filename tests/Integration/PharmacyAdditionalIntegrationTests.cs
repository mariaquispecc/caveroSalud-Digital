using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CaveroSalud.Features.Pharmacy.DTOs;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CaveroSalud.Tests.Integration
{
    public class PharmacyAdditionalIntegrationTests : IClassFixture<ApiTestFactory>
    {
        private readonly ApiTestFactory _factory;

        public PharmacyAdditionalIntegrationTests(ApiTestFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetPendingPrescriptions_ReturnsOnlyRequested()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var patient = new CaveroSalud.Domain.Entities.ApplicationUser { Id = Guid.NewGuid(), FullName = "P1", Dni = "111", PhoneNumber = string.Empty, Speciality = string.Empty };
            db.Users.Add(patient);

            var prescription = new CaveroSalud.Domain.Entities.Prescription
            {
                Id = Guid.NewGuid(),
                AppointmentId = Guid.NewGuid(),
                PatientId = patient.Id,
                DoctorId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Status = CaveroSalud.Domain.Entities.PrescriptionStatuses.Requested,
                DeliveryNotes = string.Empty,
                Items = new System.Collections.Generic.List<CaveroSalud.Domain.Entities.PrescriptionItem>
                {
                    new CaveroSalud.Domain.Entities.PrescriptionItem { Id = Guid.NewGuid(), Medication = "MedA", Dosage = "1x", Quantity = 2 }
                }
            };
            db.Prescriptions.Add(prescription);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Farmacéutico");

            var resp = await client.GetAsync("/api/v1/pharmacy/prescriptions/pending");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var results = await resp.Content.ReadFromJsonAsync<PendingPrescriptionDto[]>();
            Assert.NotNull(results);
            Assert.Contains(results, r => r.Id == prescription.Id);
        }

        [Fact]
        public async Task DeliverPrescription_ReducesInventoryAndMarksDelivered()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var patient = new CaveroSalud.Domain.Entities.ApplicationUser { Id = Guid.NewGuid(), FullName = "P2", Dni = "222", PhoneNumber = string.Empty, Speciality = string.Empty };
            db.Users.Add(patient);

            var prescription = new CaveroSalud.Domain.Entities.Prescription
            {
                Id = Guid.NewGuid(),
                AppointmentId = Guid.NewGuid(),
                PatientId = patient.Id,
                DoctorId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Status = CaveroSalud.Domain.Entities.PrescriptionStatuses.Requested,
                DeliveryNotes = string.Empty,
                Items = new System.Collections.Generic.List<CaveroSalud.Domain.Entities.PrescriptionItem>
                {
                    new CaveroSalud.Domain.Entities.PrescriptionItem { Id = Guid.NewGuid(), Medication = "MedX", Dosage = "1x", Quantity = 3 }
                }
            };
            db.Prescriptions.Add(prescription);

            var inventory = new CaveroSalud.Domain.Entities.InventoryItem { Id = Guid.NewGuid(), Name = "MedX", Unit = "u", Quantity = 10, MinThreshold = 1 };
            db.InventoryItems.Add(inventory);

            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Farmacéutico");

            var dto = new DeliverPrescriptionDto { DeliveryNotes = "OK" };
            var resp = await client.PostAsJsonAsync($"/api/v1/pharmacy/prescriptions/{prescription.Id}/deliver", dto);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var updated = await db.Prescriptions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == prescription.Id);
            Assert.Equal(CaveroSalud.Domain.Entities.PrescriptionStatuses.Delivered, updated.Status);
            var updatedInv = await db.InventoryItems.AsNoTracking().FirstAsync(i => i.Name == "MedX");
            Assert.Equal(7, updatedInv.Quantity);
        }

        [Fact]
        public async Task SearchPrescriptions_ByDni_ReturnsMatches()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var patient = new CaveroSalud.Domain.Entities.ApplicationUser { Id = Guid.NewGuid(), FullName = "P3", Dni = "333", PhoneNumber = string.Empty, Speciality = string.Empty };
            db.Users.Add(patient);

            var prescription = new CaveroSalud.Domain.Entities.Prescription
            {
                Id = Guid.NewGuid(),
                AppointmentId = Guid.NewGuid(),
                PatientId = patient.Id,
                DoctorId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Status = CaveroSalud.Domain.Entities.PrescriptionStatuses.Requested,
                DeliveryNotes = string.Empty,
                Items = new System.Collections.Generic.List<CaveroSalud.Domain.Entities.PrescriptionItem>
                {
                    new CaveroSalud.Domain.Entities.PrescriptionItem { Id = Guid.NewGuid(), Medication = "MedY", Dosage = "1x", Quantity = 1 }
                }
            };
            db.Prescriptions.Add(prescription);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Farmacéutico");

            var resp = await client.GetAsync($"/api/v1/pharmacy/prescriptions/search?dni={patient.Dni}&consultationNumber={prescription.AppointmentId}");
            var body = await resp.Content.ReadAsStringAsync();
            Assert.True(resp.IsSuccessStatusCode, $"Status: {resp.StatusCode}, Body: {body}");
            var results = await resp.Content.ReadFromJsonAsync<PendingPrescriptionDto[]>();
            Assert.NotNull(results);
            Assert.Contains(results, r => r.Id == prescription.Id);
        }

        [Fact]
        public async Task DeliverPrescription_ReturnsBadRequest_WhenInsufficientStock()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var patient = new CaveroSalud.Domain.Entities.ApplicationUser { Id = Guid.NewGuid(), FullName = "P4", Dni = "444", PhoneNumber = string.Empty, Speciality = string.Empty };
            db.Users.Add(patient);

            var prescription = new CaveroSalud.Domain.Entities.Prescription
            {
                Id = Guid.NewGuid(),
                AppointmentId = Guid.NewGuid(),
                PatientId = patient.Id,
                DoctorId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Status = CaveroSalud.Domain.Entities.PrescriptionStatuses.Requested,
                DeliveryNotes = string.Empty,
                Items = new System.Collections.Generic.List<CaveroSalud.Domain.Entities.PrescriptionItem>
                {
                    new CaveroSalud.Domain.Entities.PrescriptionItem { Id = Guid.NewGuid(), Medication = "MedZ", Dosage = "1x", Quantity = 10 }
                }
            };
            db.Prescriptions.Add(prescription);

            var inventory = new CaveroSalud.Domain.Entities.InventoryItem { Id = Guid.NewGuid(), Name = "MedZ", Unit = "u", Quantity = 5, MinThreshold = 1 };
            db.InventoryItems.Add(inventory);

            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Farmacéutico");

            var dto = new DeliverPrescriptionDto { DeliveryNotes = "OK" };
            var resp = await client.PostAsJsonAsync($"/api/v1/pharmacy/prescriptions/{prescription.Id}/deliver", dto);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task SearchPrescriptions_ReturnsBadRequest_WhenConsultationNumberInvalid()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Farmacéutico");

            var resp = await client.GetAsync("/api/v1/pharmacy/prescriptions/search?dni=&consultationNumber=invalid-guid");
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task DeliverPrescription_ReturnsNotFound_WhenPrescriptionDoesNotExist()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Farmacéutico");

            var dto = new DeliverPrescriptionDto { DeliveryNotes = "None" };
            var response = await client.PostAsJsonAsync($"/api/v1/pharmacy/prescriptions/{Guid.NewGuid()}/deliver", dto);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeliverPrescription_ReturnsBadRequest_WhenInventoryItemMissing()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var patient = new CaveroSalud.Domain.Entities.ApplicationUser { Id = Guid.NewGuid(), FullName = "P4", Dni = "444", PhoneNumber = string.Empty, Speciality = string.Empty };
            db.Users.Add(patient);

            var prescription = new CaveroSalud.Domain.Entities.Prescription
            {
                Id = Guid.NewGuid(),
                AppointmentId = Guid.NewGuid(),
                PatientId = patient.Id,
                DoctorId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Status = CaveroSalud.Domain.Entities.PrescriptionStatuses.Requested,
                DeliveryNotes = string.Empty,
                Items = new System.Collections.Generic.List<CaveroSalud.Domain.Entities.PrescriptionItem>
                {
                    new CaveroSalud.Domain.Entities.PrescriptionItem { Id = Guid.NewGuid(), Medication = "MedMissing", Dosage = "1x", Quantity = 1 }
                }
            };
            db.Prescriptions.Add(prescription);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Farmacéutico");

            var dto = new DeliverPrescriptionDto { DeliveryNotes = "OK" };
            var resp = await client.PostAsJsonAsync($"/api/v1/pharmacy/prescriptions/{prescription.Id}/deliver", dto);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }
    }
}
