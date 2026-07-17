using System;
using System.Collections.Generic;

namespace CaveroSalud.Domain.Entities
{
    public static class PrescriptionStatuses
    {
        public const string Requested = "Requested";
        public const string Delivered = "Delivered";
    }

    public class Prescription
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = PrescriptionStatuses.Requested;
        public Guid? DeliveredById { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string DeliveryNotes { get; set; }
        public List<PrescriptionItem> Items { get; set; } = new();
    }

    public class PrescriptionItem
    {
        public Guid Id { get; set; }
        public Guid PrescriptionId { get; set; }
        public Prescription Prescription { get; set; } = null!;
        public string Medication { get; set; }
        public string Dosage { get; set; }
        public int Quantity { get; set; }
    }
}
