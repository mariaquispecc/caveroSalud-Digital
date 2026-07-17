using System.Collections.Generic;

namespace CaveroSalud.Features.Doctors.DTOs
{
    public class AttendAppointmentDto
    {
        public string? Diagnosis { get; set; }
        public string? Treatment { get; set; }
        public string? Observations { get; set; }
        public List<LabOrderDto> LabOrders { get; set; } = new();
        public List<PrescriptionDto> Prescriptions { get; set; } = new();
    }

    public class LabOrderDto
    {
        public string TestName { get; set; }
    }

    public class PrescriptionDto
    {
        public string Medication { get; set; }
        public string Dosage { get; set; }
        public int Quantity { get; set; }
    }
}
