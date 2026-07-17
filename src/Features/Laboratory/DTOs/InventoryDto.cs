using System;

namespace CaveroSalud.Features.Laboratory.DTOs
{
    public class InventoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal MinThreshold { get; set; }
    }

    public class UpsertInventoryDto
    {
        public string Name { get; set; }
        public string Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal MinThreshold { get; set; }
    }
}
