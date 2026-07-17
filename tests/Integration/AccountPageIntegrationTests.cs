using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CaveroSalud.Tests.Integration
{
    public class AccountPageIntegrationTests : IClassFixture<ApiTestFactory>
    {
        private readonly ApiTestFactory _factory;

        public AccountPageIntegrationTests(ApiTestFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task PatientAccountUpdate_ChangesPasswordAndShowsSuccessMessage()
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "patient@example.com",
                Email = "patient@example.com",
                FullName = "Patient User",
                Dni = "12345678",
                PhoneNumber = "999999999",
                Speciality = string.Empty
            };

            var createResult = await userManager.CreateAsync(user, "OldPassword123!");
            Assert.True(createResult.Succeeded);

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            if (!await roleManager.RoleExistsAsync("Paciente"))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>("Paciente"));
            }
            await userManager.AddToRoleAsync(user, "Paciente");

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{user.Id}|Paciente");

            var pageResponse = await client.GetAsync("/app/paciente");
            Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);

            var pageContent = await pageResponse.Content.ReadAsStringAsync();
            var tokenMatch = Regex.Match(pageContent, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
            Assert.True(tokenMatch.Success, "Expected antiforgery token to be present in the page response.");
            var token = tokenMatch.Groups[1].Value;

            var form = new List<KeyValuePair<string, string>>
            {
                new("Account.FullName", "Patient Updated"),
                new("Account.Email", "patient.updated@example.com"),
                new("Account.Phone", "111222333"),
                new("Account.Dni", "87654321"),
                new("Account.CurrentPassword", "OldPassword123!"),
                new("Account.NewPassword", "NewPassword123!"),
                new("Account.ConfirmPassword", "NewPassword123!"),
                new("__RequestVerificationToken", token)
            };

            var postResponse = await client.PostAsync("/app/paciente?handler=UpdateAccount", new FormUrlEncodedContent(form));
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
            var responseContent = await postResponse.Content.ReadAsStringAsync();
            var decodedContent = System.Net.WebUtility.HtmlDecode(responseContent);
            Assert.Contains("Tu cuenta se actualizó correctamente.", decodedContent);

            // Verify password change - create a new scope to ensure we get fresh data from database
            using var verifyScope = _factory.Services.CreateScope();
            var verifyUserManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CaveroDbContext>();

            var updatedUser = await verifyUserManager.FindByEmailAsync("patient.updated@example.com");
            Assert.NotNull(updatedUser);
            
            var persistedUser = await verifyDb.Users.SingleOrDefaultAsync(u => u.Id == updatedUser.Id);
            Assert.False(string.IsNullOrWhiteSpace(persistedUser?.PasswordHash));
            
            // Force reload the user from database to ensure we have the latest password hash
            await verifyDb.Entry(updatedUser).ReloadAsync();
            
            var oldPasswordCheck = await verifyUserManager.CheckPasswordAsync(updatedUser, "OldPassword123!");
            var newPasswordCheck = await verifyUserManager.CheckPasswordAsync(updatedUser, "NewPassword123!");
            Assert.False(oldPasswordCheck);
            Assert.True(newPasswordCheck);
        }
    }
}
