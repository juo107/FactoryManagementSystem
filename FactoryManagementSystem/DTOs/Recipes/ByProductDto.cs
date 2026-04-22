using System.Text.Json.Serialization;

namespace FactoryManagementSystem.DTOs.Recipes
{
    public class ByProductDto
    {
        [JsonPropertyName("byProductId")]
        public long ByProductId { get; set; }

        [JsonPropertyName("processId")]
        public long ProcessId { get; set; }

        [JsonPropertyName("byProductCode")]
        public string? ByProductCode { get; set; }

        [JsonPropertyName("byProductName")]
        public string? ByProductName { get; set; }

        [JsonPropertyName("planQuantity")]
        public double? PlanQuantity { get; set; }

        [JsonPropertyName("unitOfMeasurement")]
        public string? UnitOfMeasurement { get; set; }
    }
}
