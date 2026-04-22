using System.Text.Json.Serialization;

namespace FactoryManagementSystem.DTOs.Recipes
{
    public class RecipeProductDto
    {
        [JsonPropertyName("productId")]
        public long ProductId { get; set; }

        [JsonPropertyName("processId")]
        public long ProcessId { get; set; }

        [JsonPropertyName("productCode")]
        public string? ProductCode { get; set; }

        [JsonPropertyName("planQuantity")]
        public double? PlanQuantity { get; set; }

        [JsonPropertyName("unitOfMeasurement")]
        public string? UnitOfMeasurement { get; set; }

        [JsonPropertyName("itemName")]
        public string? ItemName { get; set; }
    }
}
