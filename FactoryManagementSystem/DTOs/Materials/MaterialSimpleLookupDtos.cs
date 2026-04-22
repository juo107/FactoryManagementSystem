using System.Text.Json.Serialization;

namespace FactoryManagementSystem.DTOs.Materials
{
    public class MaterialSimpleDto
    {
        [JsonPropertyName("value")]
        public string? Value { get; set; }

        public static MaterialSimpleDto From(string? value) => new MaterialSimpleDto { Value = value };
    }

    public class MaterialProductionOrderDto
    {
        [JsonPropertyName("productionOrderNumber")]
        public string? ProductionOrderNumber { get; set; }
    }

    public class MaterialBatchDto
    {
        [JsonPropertyName("batchCode")]
        public string? BatchCode { get; set; }
    }

    public class MaterialIngredientDto
    {
        [JsonPropertyName("ingredientCode")]
        public string? IngredientCode { get; set; }
    }
}
