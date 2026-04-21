namespace FactoryManagementSystem.DTOs.Recipes
{
    public class RecipeDto
    {
        public int RecipeDetailsId { get; set; }
        public string? RecipeCode { get; set; }
        public string? RecipeName { get; set; }
        public string? RecipeVersion { get; set; }
        public string? RecipeStatus { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public string? Version { get; set; }
        public DateTime? Timestamp { get; set; }
    }
}
