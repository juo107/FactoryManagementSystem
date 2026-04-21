using System;

namespace FactoryManagementSystem.DTOs.Materials
{
    public class MaterialConsumptionDto
    {
        public int Id { get; set; }
        public string? ProductionOrderNumber { get; set; }
        public string? BatchCode { get; set; }
        public string? Quantity { get; set; }
        public string? IngredientCode { get; set; }
        public string? IngredientName { get; set; }
        public string? Lot { get; set; }
        public string? UnitOfMeasurement { get; set; }
        public DateTime? Datetime { get; set; }
        public string? Operator_ID { get; set; }
        public string? SupplyMachine { get; set; }
        public string? Respone { get; set; }
        public string? Status { get; set; }
        public string? Status1 { get; set; }
        public string? Request { get; set; }
        public int? Count { get; set; }
        public DateTime? Timestamp { get; set; }
        
        // Joined fields from ProductionOrders
        public string? Shift { get; set; }
        public string? ProductionLine { get; set; }
    }
}
