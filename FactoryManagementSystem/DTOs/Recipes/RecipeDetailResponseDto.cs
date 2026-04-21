using System.Collections.Generic;

namespace FactoryManagementSystem.DTOs.Recipes
{
    public class RecipeDetailResponseDto
    {
        public RecipeDto Recipe { get; set; } = null!;
        public IEnumerable<object> Processes { get; set; } = new List<object>();
        public IEnumerable<object> Ingredients { get; set; } = new List<object>();
        public IEnumerable<object> Products { get; set; } = new List<object>();
        public IEnumerable<object> ByProducts { get; set; } = new List<object>();
        public IEnumerable<object> Parameters { get; set; } = new List<object>();
    }
}
