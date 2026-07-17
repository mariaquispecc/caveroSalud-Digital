using System;
using CaveroSalud.Features.Pharmacy.DTOs;
using Xunit;

namespace CaveroSalud.Tests.Unit
{
    public class PharmacyDtoTests
    {
        [Fact]
        public void PharmacyDtos_PropertyAccessors_Work()
        {
            var avail = new InventoryAvailabilityDto
            {
                Id = Guid.NewGuid(),
                Name = "Item",
                Unit = "u",
                Quantity = 5,
                MinThreshold = 2,
                IsLowStock = false
            };

            Assert.Equal("Item", avail.Name);
            Assert.False(avail.IsLowStock);

            var pending = new PendingPrescriptionDto
            {
                Id = Guid.NewGuid(),
                PatientName = "Paciente",
                PatientDni = "999",
                Status = "Requested",
            };

            Assert.Equal("999", pending.PatientDni);

            var item = new PrescriptionItemDto { Medication = "X", Dosage = "1x", Quantity = 2 };
            Assert.Equal(2, item.Quantity);
        }
    }
}
