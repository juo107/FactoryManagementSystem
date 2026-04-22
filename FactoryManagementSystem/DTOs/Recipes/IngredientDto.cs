using System.Text.Json.Serialization;

namespace FactoryManagementSystem.DTOs.Recipes
{
    public class IngredientDto
    {
        [JsonPropertyName("ingredientId")]
        public long IngredientId { get; set; }

        [JsonPropertyName("processId")]
        public long ProcessId { get; set; }

        [JsonPropertyName("ingredientCode")]
        public string? IngredientCode { get; set; }

        [JsonPropertyName("itemName")]
        public string? ItemName { get; set; }

        [JsonPropertyName("quantity")]
        public double? Quantity { get; set; }

        [JsonPropertyName("unitOfMeasurement")]
        public string? UnitOfMeasurement { get; set; }
    }
}
