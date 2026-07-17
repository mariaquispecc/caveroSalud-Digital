using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CaveroSalud.Tests.Integration
{
    public class ApiTestFactory : WebApplicationFactory<Program>
    {
        private static readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CaveroDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<CaveroDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));

                // Replace real email sender with a test-friendly no-op implementation
                var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(CaveroSalud.Infrastructure.Services.IEmailSender));
                if (emailDescriptor != null)
                {
                    services.Remove(emailDescriptor);
                }
                services.AddSingleton<CaveroSalud.Infrastructure.Services.IEmailSender, TestEmailSender>();

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, options => { });
            });
        }

        // Simple test email sender to avoid real SMTP calls during integration tests
        private class TestEmailSender : CaveroSalud.Infrastructure.Services.IEmailSender
        {
            public System.Threading.Tasks.Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
            {
                // no-op in tests
                return System.Threading.Tasks.Task.CompletedTask;
            }
        }
    }

    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "Test";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-User", out var headerValues))
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing test user header."));
            }

            var payload = headerValues.ToString();
            var parts = payload.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid test user header format."));
            }

            var userId = parts[0];
            var roles = parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var claims = roles
                .Select(role => new Claim(ClaimTypes.Role, role))
                .ToList<Claim>();

            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
