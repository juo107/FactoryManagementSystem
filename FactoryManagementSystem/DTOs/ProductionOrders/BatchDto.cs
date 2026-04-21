namespace FactoryManagementSystem.DTOs.ProductionOrders
{
    public class BatchDto
    {
        public int BatchId { get; set; }
        public int ProductionOrderId { get; set; }
        public string BatchNumber { get; set; } = null!;
        public decimal? Quantity { get; set; }
        public string? UnitOfMeasurement { get; set; }
        public int? Status { get; set; }
    }
}
