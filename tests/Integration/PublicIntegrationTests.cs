using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CaveroSalud.Features.Public.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CaveroSalud.Tests.Integration
{
    public class PublicIntegrationTests : IClassFixture<ApiTestFactory>
    {
        private readonly ApiTestFactory _factory;

        public PublicIntegrationTests(ApiTestFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Contact_CreatesMessage_WhenValid()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroSalud.Infrastructure.Identity.CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var client = _factory.CreateClient();

            var dto = new ContactDto
            {
                Name = "Alice",
                Email = "alice@example.com",
                Phone = "555-1111",
                Message = "Hello"
            };

            var response = await client.PostAsJsonAsync("/api/v1/public/contact", dto);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var msg = await db.ContactMessages.FirstOrDefaultAsync(m => m.Email == "alice@example.com");
            Assert.NotNull(msg);
            Assert.Equal("Hello", msg.Message);
        }

        [Fact]
        public async Task RootRoute_ReturnsHomePage()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Tu salud es nuestra prioridad", html);
        }
    }
}
