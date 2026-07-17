using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CaveroSalud.Features.Administration.DTOs;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CaveroSalud.Tests.Integration
{
    public class AdministrationIntegrationTests : IClassFixture<ApiTestFactory>
    {
        private readonly ApiTestFactory _factory;

        public AdministrationIntegrationTests(ApiTestFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateSpeciality_ReturnsOk_WhenValid()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var adminId = Guid.NewGuid();
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{adminId}|Administrador");

            var dto = new SpecialityDto
            {
                Name = "Cardiology",
                Description = "Heart specialty",
                IsActive = true
            };

            var response = await client.PostAsJsonAsync("/api/v1/admin/specialities", dto);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var payload = await response.Content.ReadFromJsonAsync<object>();
            Assert.NotNull(payload);

            var speciality = await db.Specialities.FirstOrDefaultAsync(s => s.Name == "Cardiology");
            Assert.NotNull(speciality);
        }
    }
}
