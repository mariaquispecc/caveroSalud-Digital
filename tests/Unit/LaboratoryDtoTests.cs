using System;
using CaveroSalud.Features.Laboratory.DTOs;
using Xunit;

namespace CaveroSalud.Tests.Unit
{
    public class LaboratoryDtoTests
    {
        [Fact]
        public void LaboratoryDtos_PropertyAccessors_Work()
        {
            var inv = new InventoryDto { Id = Guid.NewGuid(), Name = "Reagent", Unit = "ml", Quantity = 10, MinThreshold = 1 };
            Assert.Equal("Reagent", inv.Name);

            var order = new LabOrderDto { Id = Guid.NewGuid(), TestName = "CBC", Status = "Requested" };
            Assert.Equal("CBC", order.TestName);

            var item = new LabResultItemDto { Analyte = "Na", Value = "140", Unit = "mmol/L" };
            Assert.Equal("140", item.Value);
        }
    }
}
