using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FactoryManagementSystem.DTOs.ProductionOrders
{
    public class BatchDto
    {
        [JsonPropertyName("batchId")]
        public int BatchId { get; set; }

        [JsonPropertyName("productionOrderId")]
        public int ProductionOrderId { get; set; }

        [JsonPropertyName("batchNumber")]
        [Required]
        [MinLength(1)]
        public string BatchNumber { get; set; } = null!;

        [JsonPropertyName("quantity")]
        public decimal? Quantity { get; set; }

        [JsonPropertyName("unitOfMeasurement")]
        public string? UnitOfMeasurement { get; set; }

        [JsonPropertyName("status")]
        public int? Status { get; set; }
    }
}
