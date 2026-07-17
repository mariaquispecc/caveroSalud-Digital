using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CaveroSalud.Features.Pharmacy.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CaveroSalud.Tests.Integration
{
    public class PharmacyIntegrationTests : IClassFixture<ApiTestFactory>
    {
        private readonly ApiTestFactory _factory;

        public PharmacyIntegrationTests(ApiTestFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetInventoryAvailability_ReturnsItems_WhenExists()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroSalud.Infrastructure.Identity.CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // Seed an inventory item
            var item = new CaveroSalud.Domain.Entities.InventoryItem
            {
                Id = Guid.NewGuid(),
                Name = "Paracetamol",
                Unit = "tablet",
                Quantity = 50,
                MinThreshold = 10
            };
            db.InventoryItems.Add(item);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Farmacéutico");

            var response = await client.GetAsync("/api/v1/pharmacy/inventory");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var results = await response.Content.ReadFromJsonAsync<InventoryAvailabilityDto[]>();
            Assert.NotNull(results);
            Assert.Contains(results, r => r.Name == "Paracetamol");
        }
    }
}
