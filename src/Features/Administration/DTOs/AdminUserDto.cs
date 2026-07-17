using System;

namespace CaveroSalud.Features.Administration.DTOs
{
    public class AdminUserDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Dni { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public string Speciality { get; set; }
        public bool IsActive { get; set; }
    }

    public class ManageUserDto
    {
        public string FullName { get; set; }
        public string Dni { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public string Speciality { get; set; }
        public bool IsActive { get; set; }
        public string? TemporaryPassword { get; set; }
    }

    public class AppointmentActionDto
    {
        public Guid AppointmentId { get; set; }
        public DateTime? NewStartAt { get; set; }
        public DateTime? NewEndAt { get; set; }
        public string Notes { get; set; }
    }

    public class SpecialityDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class PublicInfoDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string TagLine { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }

    public class AdminSummaryDto
    {
        public int TodayAppointments { get; set; }
        public int ActiveStaff { get; set; }
        public int PendingAppointments { get; set; }
        public int PrescriptionsCount { get; set; }
        public int LabOrdersCount { get; set; }
        public int LowStockItems { get; set; }
    }
}
