using System;

namespace CaveroSalud.Domain.Entities
{
    public class InventoryItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal MinThreshold { get; set; }
    }
}
