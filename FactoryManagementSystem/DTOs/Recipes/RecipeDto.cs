using System;
using System.Text.Json.Serialization;

namespace FactoryManagementSystem.DTOs.Recipes
{
    public class RecipeDto
    {
        [JsonPropertyName("recipeDetailsId")]
        public int RecipeDetailsId { get; set; }

        [JsonPropertyName("recipeCode")]
        public string? RecipeCode { get; set; }

        [JsonPropertyName("recipeName")]
        public string? RecipeName { get; set; }

        [JsonPropertyName("recipeVersion")]
        public string? RecipeVersion { get; set; }

        [JsonPropertyName("recipeStatus")]
        public string? RecipeStatus { get; set; }

        [JsonPropertyName("productCode")]
        public string? ProductCode { get; set; }

        [JsonPropertyName("productName")]
        public string? ProductName { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }
    }
}
