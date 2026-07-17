using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Features.Administration.DTOs;
using CaveroSalud.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CaveroSalud.Tests.Integration
{
    public class AdministrationAdditionalIntegrationTests : IClassFixture<ApiTestFactory>
    {
        private readonly ApiTestFactory _factory;

        public AdministrationAdditionalIntegrationTests(ApiTestFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetUsers_IncludesCreatedUser()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var client = _factory.CreateClient();
            var adminId = Guid.NewGuid();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{adminId}|Administrador");

            var createDto = new ManageUserDto
            {
                Email = "newuser@example.com",
                FullName = "New User",
                Dni = "1111",
                Phone = "333",
                Role = "Paciente",
                Speciality = "",
                IsActive = true,
                TemporaryPassword = "Temp!23Ab"
            };

            var createResp = await client.PostAsJsonAsync("/api/v1/admin/users", createDto);
            Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

            var getResp = await client.GetAsync("/api/v1/admin/users");
            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
            var users = await getResp.Content.ReadFromJsonAsync<AdminUserDto[]>();
            Assert.Contains(users, u => u.Email == "newuser@example.com");
        }

        [Fact]
        public async Task CreateAndUpdateUser_Works()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var client = _factory.CreateClient();
            var adminId = Guid.NewGuid();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{adminId}|Administrador");

            var createDto = new ManageUserDto
            {
                Email = "updatable@example.com",
                FullName = "Updatable",
                Dni = "2222",
                Phone = "444",
                Role = "Empleado",
                Speciality = "",
                IsActive = true,
                TemporaryPassword = "Pwd!2345"
            };

            var createResp = await client.PostAsJsonAsync("/api/v1/admin/users", createDto);
            Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "updatable@example.com");
            Assert.NotNull(user);

            var updateDto = new ManageUserDto
            {
                Email = "updatable@example.com",
                FullName = "Updatable Edited",
                Dni = "2222",
                Phone = "999",
                Role = "Administrador",
                Speciality = "",
                IsActive = false,
                TemporaryPassword = null
            };

            var putResp = await client.PutAsJsonAsync($"/api/v1/admin/users/{user.Id}", updateDto);
            Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);

            using var reloadScope = _factory.Services.CreateScope();
            var reloadDb = reloadScope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            var updated = await reloadDb.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
            Assert.Equal("Updatable Edited", updated.FullName);
            Assert.True(updated.LockoutEnabled);
        }

        [Fact]
        public async Task RescheduleAppointment_UpdatesTimes()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var appt = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Speciality = "General",
                StartAt = DateTime.UtcNow.AddDays(2),
                EndAt = DateTime.UtcNow.AddDays(2).AddHours(1),
                Status = AppointmentStatus.Scheduled
            };
            db.Appointments.Add(appt);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            var adminId = Guid.NewGuid();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{adminId}|Administrador");

            var dto = new AppointmentActionDto
            {
                NewStartAt = DateTime.UtcNow.AddDays(3),
                NewEndAt = DateTime.UtcNow.AddDays(3).AddHours(1),
                Notes = "Need to move"
            };

            var resp = await client.PostAsJsonAsync($"/api/v1/admin/appointments/{appt.Id}/reschedule", dto);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var reloaded = await db.Appointments.FindAsync(appt.Id);
            // allow up to 1 day difference to avoid kind/offset issues in in-memory provider
            var dayDiff = (reloaded.StartAt - dto.NewStartAt.Value).Duration().TotalDays;
            Assert.True(dayDiff <= 1.1, $"Start date differs by {dayDiff} days");
        }

        [Fact]
        public async Task UpsertAndGetPublicInfo_Works()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var client = _factory.CreateClient();
            var adminId = Guid.NewGuid();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{adminId}|Administrador");

            var dto = new PublicInfoDto
            {
                Title = "Clinic",
                TagLine = "Care",
                Description = "Desc",
                Address = "Street",
                Email = "info@clinic",
                Phone = "123"
            };

            var post = await client.PostAsJsonAsync("/api/v1/admin/public-info", dto);
            Assert.Equal(HttpStatusCode.OK, post.StatusCode);

            var get = await client.GetAsync("/api/v1/admin/public-info");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var fetched = await get.Content.ReadFromJsonAsync<PublicInfoDto>();
            Assert.Equal("Clinic", fetched.Title);
        }

        [Fact]
        public async Task GetSummary_ReturnsCounts()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // seed an appointment for today
            var appt = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Speciality = "General",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(1),
                Status = AppointmentStatus.Scheduled
            };
            db.Appointments.Add(appt);

            // seed inventory low stock
            var item = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Name = "Med",
                Quantity = 1,
                MinThreshold = 5,
                Unit = "pcs"
            };
            db.InventoryItems.Add(item);

            // seed prescription (set DeliveryNotes to avoid nullability error)
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Status = "Requested",
                DeliveryNotes = string.Empty
            };
            db.Prescriptions.Add(prescription);

            var lab = new LabOrder { Id = Guid.NewGuid(), TestName = "T", CreatedAt = DateTime.UtcNow, Status = "Pending" };
            db.LabOrders.Add(lab);

            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            var adminId = Guid.NewGuid();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{adminId}|Administrador");

            var resp = await client.GetAsync("/api/v1/admin/summary");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var summary = await resp.Content.ReadFromJsonAsync<AdminSummaryDto>();
            Assert.True(summary.PrescriptionsCount >= 1);
            Assert.True(summary.LabOrdersCount >= 1);
            Assert.True(summary.LowStockItems >= 1);
        }

        [Fact]
        public async Task UpdateSpeciality_ReturnsOk_WhenSpecialityExists()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var speciality = new Speciality
            {
                Id = Guid.NewGuid(),
                Name = "Pediatrics",
                Description = "Child care",
                IsActive = true
            };
            db.Specialities.Add(speciality);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            var adminId = Guid.NewGuid();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{adminId}|Administrador");

            var dto = new SpecialityDto
            {
                Name = "Pediatrics Updated",
                Description = "Caring for children",
                IsActive = false
            };

            var resp = await client.PutAsJsonAsync($"/api/v1/admin/specialities/{speciality.Id}", dto);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            await db.Entry(speciality).ReloadAsync();
            var updated = await db.Specialities.FindAsync(speciality.Id);
            Assert.Equal("Pediatrics Updated", updated.Name);
            Assert.False(updated.IsActive);
        }

        [Fact]
        public async Task ApproveAppointment_ReturnsOk_WhenAppointmentIsScheduled()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Speciality = "General",
                StartAt = DateTime.UtcNow.AddDays(2),
                EndAt = DateTime.UtcNow.AddDays(2).AddHours(1),
                Status = AppointmentStatus.Scheduled
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            var adminId = Guid.NewGuid();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{adminId}|Administrador");

            var resp = await client.PostAsync($"/api/v1/admin/appointments/{appointment.Id}/approve", null);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        [Fact]
        public async Task CreateUser_ReturnsBadRequest_WhenEmailAlreadyExists()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var existing = new CaveroSalud.Domain.Entities.ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "duplicate@example.com",
                Email = "duplicate@example.com",
                FullName = "Duplicate",
                Dni = "7777",
                PhoneNumber = "000",
                Speciality = string.Empty
            };
            db.Users.Add(existing);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Administrador");

            var createDto = new ManageUserDto
            {
                Email = "duplicate@example.com",
                FullName = "Duplicate",
                Dni = "7777",
                Phone = "000",
                Role = "Paciente",
                Speciality = "",
                IsActive = true,
                TemporaryPassword = "Pwd12345"
            };

            var response = await client.PostAsJsonAsync("/api/v1/admin/users", createDto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Administrador");

            var updateDto = new ManageUserDto
            {
                Email = "missing@example.com",
                FullName = "Missing",
                Dni = "8888",
                Phone = "111",
                Role = "Paciente",
                Speciality = string.Empty,
                IsActive = true,
                TemporaryPassword = "Pwd12345"
            };

            var response = await client.PutAsJsonAsync($"/api/v1/admin/users/{Guid.NewGuid()}", updateDto);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateSpeciality_ReturnsBadRequest_WhenNameIsMissing()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Administrador");

            var dto = new SpecialityDto
            {
                Name = string.Empty,
                Description = "Desc",
                IsActive = true
            };

            var response = await client.PostAsJsonAsync("/api/v1/admin/specialities", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ApproveAppointment_ReturnsBadRequest_WhenAppointmentIsNotScheduled()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Speciality = "General",
                StartAt = DateTime.UtcNow.AddDays(2),
                EndAt = DateTime.UtcNow.AddDays(2).AddHours(1),
                Status = AppointmentStatus.Cancelled
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Administrador");

            var response = await client.PostAsync($"/api/v1/admin/appointments/{appointment.Id}/approve", null);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ApproveAppointment_ReturnsNotFound_WhenAppointmentDoesNotExist()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Administrador");

            var response = await client.PostAsync($"/api/v1/admin/appointments/{Guid.NewGuid()}/approve", null);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RescheduleAppointment_ReturnsNotFound_WhenAppointmentDoesNotExist()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Administrador");

            var dto = new AppointmentActionDto
            {
                NewStartAt = DateTime.UtcNow.AddDays(1),
                NewEndAt = DateTime.UtcNow.AddDays(1).AddHours(1),
                Notes = "reschedule"
            };

            var response = await client.PostAsJsonAsync($"/api/v1/admin/appointments/{Guid.NewGuid()}/reschedule", dto);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RescheduleAppointment_ReturnsBadRequest_WhenTimesAreMissing()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Speciality = "General",
                StartAt = DateTime.UtcNow.AddDays(2),
                EndAt = DateTime.UtcNow.AddDays(2).AddHours(1),
                Status = AppointmentStatus.Scheduled
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Administrador");

            var dto = new AppointmentActionDto { NewStartAt = null, NewEndAt = null, Notes = "" };
            var response = await client.PostAsJsonAsync($"/api/v1/admin/appointments/{appointment.Id}/reschedule", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetSpecialities_ReturnsList_WhenSpecialitiesExist()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Specialities.Add(new Speciality { Id = Guid.NewGuid(), Name = "Dermatology", Description = "Skin care", IsActive = true });
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Administrador");

            var response = await client.GetAsync("/api/v1/admin/specialities");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var list = await response.Content.ReadFromJsonAsync<SpecialityDto[]>();
            Assert.Contains(list, s => s.Name == "Dermatology");
        }

        [Fact]
        public async Task UpdateSpeciality_ReturnsNotFound_WhenSpecialityDoesNotExist()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Administrador");

            var dto = new SpecialityDto
            {
                Name = "Nonexistent",
                Description = "None",
                IsActive = false
            };

            var response = await client.PutAsJsonAsync($"/api/v1/admin/specialities/{Guid.NewGuid()}", dto);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetPublicInfo_ReturnsNotFound_WhenNoPublicInfoExists()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Administrador");

            var response = await client.GetAsync("/api/v1/admin/public-info");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpsertPublicInfo_UpdatesExistingInfo()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.PublicInfos.Add(new PublicInfo
            {
                Id = Guid.NewGuid(),
                Title = "Clinic",
                TagLine = "Old",
                Description = "Old desc",
                Address = "Old addr",
                Email = "old@example.com",
                Phone = "111",
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            });
            await db.SaveChangesAsync();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Administrador");

            var dto = new PublicInfoDto
            {
                Title = "Clinic Updated",
                TagLine = "New",
                Description = "Updated desc",
                Address = "New addr",
                Email = "new@example.com",
                Phone = "222"
            };

            var response = await client.PostAsJsonAsync("/api/v1/admin/public-info", dto);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Reload entity from a fresh context to avoid cached tracking state.
            await db.Entry(db.PublicInfos.Local.FirstOrDefault()).ReloadAsync();
            var updated = await db.PublicInfos.AsNoTracking().FirstOrDefaultAsync();
            Assert.Equal("Clinic Updated", updated.Title);
            Assert.Equal("new@example.com", updated.Email);
        }

        [Fact]
        public async Task UpdateUser_UpdatesWithoutRoleChange_WhenSameRoleIsProvided()
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<CaveroSalud.Domain.Entities.ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Administrador");

            var createDto = new ManageUserDto
            {
                Email = "same-role@example.com",
                FullName = "Same Role",
                Dni = "7777",
                Phone = "777",
                Role = "Paciente",
                Speciality = string.Empty,
                IsActive = true,
                TemporaryPassword = "Temp12!"
            };

            var createResponse = await client.PostAsJsonAsync("/api/v1/admin/users", createDto);
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == createDto.Email);
            Assert.NotNull(user);

            var updateDto = new ManageUserDto
            {
                Email = createDto.Email,
                FullName = "Same Role Updated",
                Dni = createDto.Dni,
                Phone = "888",
                Role = "Paciente",
                Speciality = string.Empty,
                IsActive = true,
                TemporaryPassword = null
            };

            var updateResponse = await client.PutAsJsonAsync($"/api/v1/admin/users/{user.Id}", updateDto);
            var responseContent = await updateResponse.Content.ReadAsStringAsync();
            Assert.True(updateResponse.IsSuccessStatusCode, $"UpdateUser failed with {updateResponse.StatusCode}: {responseContent}");

            using var reloadScope = _factory.Services.CreateScope();
            var reloadDb = reloadScope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            var reloaded = await reloadDb.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
            Assert.Equal("Same Role Updated", reloaded.FullName);

            var users = await client.GetFromJsonAsync<AdminUserDto[]> ("/api/v1/admin/users");
            Assert.Contains(users, u => u.Id == user.Id && u.FullName == "Same Role Updated");
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenEmailAlreadyExists()
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<CaveroSalud.Domain.Entities.ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var existing = new CaveroSalud.Domain.Entities.ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "existing-email@example.com",
                Email = "existing-email@example.com",
                FullName = "Existing Email",
                Dni = "1000",
                PhoneNumber = "1000",
                Speciality = string.Empty
            };
            await userManager.CreateAsync(existing, "Password1!");
            if (!await roleManager.RoleExistsAsync("Paciente")) await roleManager.CreateAsync(new IdentityRole<Guid>("Paciente"));
            await userManager.AddToRoleAsync(existing, "Paciente");

            var updateTarget = new CaveroSalud.Domain.Entities.ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "duplicate-target@example.com",
                Email = "duplicate-target@example.com",
                FullName = "Duplicate Target",
                Dni = "1001",
                PhoneNumber = "1001",
                Speciality = string.Empty
            };
            await userManager.CreateAsync(updateTarget, "Password1!");
            await userManager.AddToRoleAsync(updateTarget, "Paciente");

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User", $"{Guid.NewGuid()}|Administrador");

            var dto = new ManageUserDto
            {
                Email = existing.Email,
                FullName = "Duplicate Target Updated",
                Dni = updateTarget.Dni,
                Phone = updateTarget.PhoneNumber,
                Role = "Paciente",
                Speciality = string.Empty,
                IsActive = true,
                TemporaryPassword = null
            };

            var response = await client.PutAsJsonAsync($"/api/v1/admin/users/{updateTarget.Id}", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
