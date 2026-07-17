using System;
using System.Collections.Generic;

namespace CaveroSalud.Features.ClinicalHistory.DTOs
{
    public class ClinicalRecordDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; }
        public string Speciality { get; set; }
        public string Diagnosis { get; set; }
        public string Treatment { get; set; }
        public string Observations { get; set; }
        public bool IsClosed { get; set; }
        public IEnumerable<object> LabResults { get; set; }
        public IEnumerable<object> Prescriptions { get; set; }
    }
}
