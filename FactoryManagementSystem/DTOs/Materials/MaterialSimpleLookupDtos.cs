namespace FactoryManagementSystem.DTOs.Materials
{
    public class MaterialSimpleDto
    {
        public string? Value { get; set; }

        public static MaterialSimpleDto From(string? value) => new MaterialSimpleDto { Value = value };
    }

    public class MaterialProductionOrderDto
    {
        public string? ProductionOrderNumber { get; set; }
    }

    public class MaterialBatchDto
    {
        public string? BatchCode { get; set; }
    }

    public class MaterialIngredientDto
    {
        public string? IngredientCode { get; set; }
    }
}
