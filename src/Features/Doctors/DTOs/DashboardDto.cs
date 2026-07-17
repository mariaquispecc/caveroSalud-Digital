using System.Collections.Generic;

namespace CaveroSalud.Features.Doctors.DTOs
{
    public class DashboardDto
    {
        public IEnumerable<object> TodayAppointments { get; set; }
        public IEnumerable<object> WeekAppointments { get; set; }
        public int PatientsSeenCount { get; set; }
        public int PrescriptionsIssuedCount { get; set; }
    }
}
