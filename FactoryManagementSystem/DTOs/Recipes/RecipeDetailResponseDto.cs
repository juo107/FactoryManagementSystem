using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FactoryManagementSystem.DTOs.Recipes
{
    public class RecipeDetailResponseDto
    {
        [JsonPropertyName("recipe")]
        public RecipeDto Recipe { get; set; } = null!;

        [JsonPropertyName("processes")]
        public IEnumerable<ProcessDto> Processes { get; set; } = new List<ProcessDto>();

        [JsonPropertyName("ingredients")]
        public IEnumerable<IngredientDto> Ingredients { get; set; } = new List<IngredientDto>();

        [JsonPropertyName("products")]
        public IEnumerable<RecipeProductDto> Products { get; set; } = new List<RecipeProductDto>();

        [JsonPropertyName("byProducts")]
        public IEnumerable<ByProductDto> ByProducts { get; set; } = new List<ByProductDto>();

        [JsonPropertyName("parameters")]
        public IEnumerable<ParameterDto> Parameters { get; set; } = new List<ParameterDto>();
    }
}
