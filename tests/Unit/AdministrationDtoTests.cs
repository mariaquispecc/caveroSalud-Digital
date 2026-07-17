using System;
using CaveroSalud.Features.Administration.DTOs;
using Xunit;

namespace CaveroSalud.Tests.Unit
{
    public class AdministrationDtoTests
    {
        [Fact]
        public void AdminDtos_PropertyAccessors_Work()
        {
            var summary = new AdminSummaryDto
            {
                TodayAppointments = 1,
                ActiveStaff = 2,
                PendingAppointments = 3,
                PrescriptionsCount = 4,
                LabOrdersCount = 5,
                LowStockItems = 6
            };

            Assert.Equal(1, summary.TodayAppointments);
            Assert.Equal(6, summary.LowStockItems);

            var user = new AdminUserDto
            {
                Id = Guid.NewGuid(),
                FullName = "Alice",
                Dni = "123",
                Email = "a@x.com",
                Phone = "555",
                Speciality = "Cardio",
                IsActive = true,
                Role = "Administrador"
            };

            Assert.Contains("Alice", user.FullName);

            var action = new AppointmentActionDto
            {
                Notes = "Change",
                NewStartAt = DateTime.UtcNow.AddDays(1),
                NewEndAt = DateTime.UtcNow.AddDays(1).AddHours(1)
            };
            Assert.NotNull(action.NewStartAt);
            Assert.Equal("Change", action.Notes);

            var manage = new ManageUserDto
            {
                Email = "u@x.com",
                FullName = "User",
                Dni = "987",
                Phone = "000",
                Role = "Paciente",
                Speciality = "",
                IsActive = true,
                TemporaryPassword = "tmp123"
            };
            Assert.Equal("Paciente", manage.Role);

            var info = new PublicInfoDto
            {
                Title = "Title",
                Description = "Desc",
                Address = "Addr",
                Email = "mail@x",
                Phone = "9"
            };
            Assert.Equal("Desc", info.Description);
        }
    }
}
