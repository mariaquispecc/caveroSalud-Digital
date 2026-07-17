using System;
using System.Collections.Generic;

namespace CaveroSalud.Features.Pharmacy.DTOs
{
    public class PendingPrescriptionDto
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }
        public string PatientDni { get; set; }
        public Guid DoctorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public List<PrescriptionItemDto> Items { get; set; }
    }

    public class PrescriptionItemDto
    {
        public string Medication { get; set; }
        public string Dosage { get; set; }
        public int Quantity { get; set; }
    }

    public class InventoryAvailabilityDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal MinThreshold { get; set; }
        public bool IsLowStock { get; set; }
    }

    public class DeliverPrescriptionDto
    {
        public string DeliveryNotes { get; set; }
    }
}
