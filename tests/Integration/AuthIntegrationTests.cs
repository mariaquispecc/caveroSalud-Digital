using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CaveroSalud.Features.Authentication.DTOs;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CaveroSalud.Tests.Integration
{
    public class AuthIntegrationTests : IClassFixture<ApiTestFactory>
    {
        private readonly ApiTestFactory _factory;

        public AuthIntegrationTests(ApiTestFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Register_ReturnsOk_WhenValid()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var client = _factory.CreateClient();

            var dto = new RegisterPatientDto
            {
                Email = "testpatient@example.com",
                Password = "Password123!",
                FullName = "Test Patient",
                Dni = "12345678",
                Phone = "555-0000"
            };

            var response = await client.PostAsJsonAsync("/api/v1/auth/register", dto);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var payload = await response.Content.ReadFromJsonAsync<object>();
            Assert.NotNull(payload);

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "testpatient@example.com");
            Assert.NotNull(user);
        }
    }
}
