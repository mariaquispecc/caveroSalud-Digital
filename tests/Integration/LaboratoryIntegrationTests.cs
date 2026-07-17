using System;
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
    public class LaboratoryIntegrationTests : IClassFixture<ApiTestFactory>
    {
        private readonly ApiTestFactory _factory;

        public LaboratoryIntegrationTests(ApiTestFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task UpsertInventory_AddsItem_WhenValid()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroSalud.Infrastructure.Identity.CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Laboratorista");

            var dto = new UpsertInventoryDto
            {
                Name = "TestReagent",
                Unit = "ml",
                Quantity = 100,
                MinThreshold = 10
            };

            var response = await client.PostAsJsonAsync("/api/v1/lab/inventory", dto);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var item = await db.InventoryItems.FirstOrDefaultAsync(i => i.Name == "TestReagent");
            Assert.NotNull(item);
            Assert.Equal(100, item.Quantity);
        }

        [Fact]
        public async Task GetInventory_ReturnsItems_WhenExists()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroSalud.Infrastructure.Identity.CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var item = new CaveroSalud.Domain.Entities.InventoryItem
            {
                Id = Guid.NewGuid(),
                Name = "TestChem",
                Unit = "ml",
                Quantity = 5,
                MinThreshold = 2
            };
            db.InventoryItems.Add(item);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Laboratorista");

            var response = await client.GetAsync("/api/v1/lab/inventory");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var items = await response.Content.ReadFromJsonAsync<InventoryDto[]>();
            Assert.NotNull(items);
            Assert.Contains(items, i => i.Name == "TestChem");
        }

        [Fact]
        public async Task UpdateInventory_UpdatesExistingInventoryItem()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroSalud.Infrastructure.Identity.CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var item = new CaveroSalud.Domain.Entities.InventoryItem
            {
                Id = Guid.NewGuid(),
                Name = "Resistor",
                Unit = "pcs",
                Quantity = 20,
                MinThreshold = 5
            };
            db.InventoryItems.Add(item);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Laboratorista");

            var dto = new UpsertInventoryDto
            {
                Name = "Resistor",
                Unit = "pcs",
                Quantity = 30,
                MinThreshold = 10
            };

            var response = await client.PutAsJsonAsync($"/api/v1/lab/inventory/{item.Id}", dto);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await db.Entry(item).ReloadAsync();
            var updated = await db.InventoryItems.FindAsync(item.Id);
            Assert.Equal(30, updated.Quantity);
            Assert.Equal(10, updated.MinThreshold);
        }
    }
}
