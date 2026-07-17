using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CaveroSalud.Features.Laboratory.DTOs;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CaveroSalud.Tests.Integration
{
    public class LaboratoryAdditionalIntegrationTests : IClassFixture<ApiTestFactory>
    {
        private readonly ApiTestFactory _factory;

        public LaboratoryAdditionalIntegrationTests(ApiTestFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task PendingOrders_ReturnsRequestedOrders()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var order = new CaveroSalud.Domain.Entities.LabOrder
            {
                Id = Guid.NewGuid(),
                AppointmentId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                TestName = "CBC",
                Status = "Requested",
                CreatedAt = DateTime.UtcNow
            };
            db.LabOrders.Add(order);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Laboratorista");

            var resp = await client.GetAsync("/api/v1/lab/orders/pending");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var results = await resp.Content.ReadFromJsonAsync<LabOrderDto[]>();
            Assert.Contains(results, r => r.Id == order.Id);
        }

        [Fact]
        public async Task SubmitResults_AddsResultsAndReports()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var order = new CaveroSalud.Domain.Entities.LabOrder
            {
                Id = Guid.NewGuid(),
                AppointmentId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                TestName = "CMP",
                Status = "Requested",
                CreatedAt = DateTime.UtcNow
            };
            db.LabOrders.Add(order);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Laboratorista");

            var dto = new SubmitResultsDto
            {
                Results = new System.Collections.Generic.List<LabResultItemDto>
                {
                    new LabResultItemDto { Analyte = "Na", Value = "140", Unit = "mmol/L", ReferenceRange = "135-145", Comments = "ok" }
                }
            };

            var resp = await client.PostAsJsonAsync($"/api/v1/lab/orders/{order.Id}/results", dto);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var updated = await db.LabOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == order.Id);
            Assert.Equal("Reported", updated.Status);
            var results = await db.LabResults.AsNoTracking().Where(r => r.LabOrderId == order.Id).ToListAsync();
            Assert.Single(results);
        }

        [Fact]
        public async Task ValidateReport_PublishesResultsAndValidatesOrder()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var order = new CaveroSalud.Domain.Entities.LabOrder
            {
                Id = Guid.NewGuid(),
                AppointmentId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                TestName = "BMP",
                Status = "Requested",
                CreatedAt = DateTime.UtcNow
            };
            db.LabOrders.Add(order);

            var result = new CaveroSalud.Domain.Entities.LabResult
            {
                Id = Guid.NewGuid(),
                LabOrderId = order.Id,
                Analyte = "K",
                Value = "4",
                Unit = "mmol/L",
                ReferenceRange = "3.5-5.0",
                Comments = "",
                CreatedAt = DateTime.UtcNow,
                Published = false
            };
            db.LabResults.Add(result);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Laboratorista");

            var resp = await client.PostAsync($"/api/v1/lab/orders/{order.Id}/validate", null);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var updatedResults = await db.LabResults.AsNoTracking().Where(r => r.LabOrderId == order.Id).ToListAsync();
            Assert.All(updatedResults, r => Assert.True(r.Published));
            var updatedOrder = await db.LabOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == order.Id);
            Assert.Equal("Validated", updatedOrder.Status);
        }

        [Fact]
        public async Task SubmitResults_ReturnsNotFound_WhenOrderMissing()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Laboratorista");

            var dto = new SubmitResultsDto { Results = new System.Collections.Generic.List<LabResultItemDto>() };
            var resp = await client.PostAsJsonAsync($"/api/v1/lab/orders/{Guid.NewGuid()}/results", dto);
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task SubmitResults_ReturnsBadRequest_WhenOrderNotRequested()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var order = new CaveroSalud.Domain.Entities.LabOrder
            {
                Id = Guid.NewGuid(),
                AppointmentId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                TestName = "CMP",
                Status = "Reported",
                CreatedAt = DateTime.UtcNow
            };
            db.LabOrders.Add(order);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Laboratorista");

            var dto = new SubmitResultsDto { Results = new System.Collections.Generic.List<LabResultItemDto> { new LabResultItemDto { Analyte = "Na", Value = "140", Unit = "mmol/L", ReferenceRange = "135-145", Comments = "ok" } } };
            var resp = await client.PostAsJsonAsync($"/api/v1/lab/orders/{order.Id}/results", dto);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task ValidateReport_ReturnsNotFound_WhenOrderMissing()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Laboratorista");

            var resp = await client.PostAsync($"/api/v1/lab/orders/{Guid.NewGuid()}/validate", null);
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task ValidateReport_ReturnsBadRequest_WhenNoResults()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var order = new CaveroSalud.Domain.Entities.LabOrder
            {
                Id = Guid.NewGuid(),
                AppointmentId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                TestName = "BMP",
                Status = "Requested",
                CreatedAt = DateTime.UtcNow
            };
            db.LabOrders.Add(order);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Laboratorista");

            var resp = await client.PostAsync($"/api/v1/lab/orders/{order.Id}/validate", null);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }
    }
}
