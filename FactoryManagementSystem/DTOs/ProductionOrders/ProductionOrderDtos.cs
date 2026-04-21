using System;

namespace FactoryManagementSystem.DTOs.ProductionOrders
{
    public class ProductionOrderDto
    {
        public int ProductionOrderId { get; set; }
        public string? ProductionLine { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; } // Joined field
        public string ProductionOrderNumber { get; set; } = null!;
        public string RecipeCode { get; set; } = null!;
        public string? RecipeName { get; set; } // Joined field
        public string RecipeVersion { get; set; } = null!;
        public string? Shift { get; set; }
        public DateTime? PlannedStart { get; set; }
        public DateTime? PlannedEnd { get; set; }
        public decimal? Quantity { get; set; }
        public string? UnitOfMeasurement { get; set; }
        public string? LotNumber { get; set; }
        public string? ProcessArea { get; set; }
        public int? Status { get; set; }
        public string? CurrentBatch { get; set; }
        public int TotalBatches { get; set; }
        public IEnumerable<BatchDto> Batches { get; set; } = new List<BatchDto>();
    }

    public class OrderStatsDto
    {
        public int Total { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public int Stopped { get; set; }
    }

    public class OrderFiltersDto
    {
        public IEnumerable<string> ProcessAreas { get; set; } = new List<string>();
        public IEnumerable<string> Shifts { get; set; } = new List<string>();
        public IEnumerable<string> ProductionOrderNumbers { get; set; } = new List<string>();
    }
}
