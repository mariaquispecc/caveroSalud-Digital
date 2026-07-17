using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.AspNetCore.Identity;
using CaveroSalud.Domain.Entities;

#nullable disable

namespace CaveroSalud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SetSeedPasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Set a known password for seeded patient users so they can log in during QA.
            var hasher = new PasswordHasher<ApplicationUser>();
            var pwd = "P@ssw0rd!";
            var u = new ApplicationUser();
            var hash = hasher.HashPassword(u, pwd)?.Replace("'", "''");
            if (!string.IsNullOrEmpty(hash))
            {
                migrationBuilder.Sql($"UPDATE \"AspNetUsers\" SET \"PasswordHash\" = '{hash}' WHERE \"Id\" = 'ec8b7dbd-82fb-4a08-90a0-90d2dd17e592';");
                migrationBuilder.Sql($"UPDATE \"AspNetUsers\" SET \"PasswordHash\" = '{hash}' WHERE \"Id\" = 'b2f9c4d4-3c6f-4d6a-9e2a-3f6c5e9a8b7c';");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
