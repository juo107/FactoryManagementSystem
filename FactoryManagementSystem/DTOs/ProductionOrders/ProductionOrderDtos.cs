using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FactoryManagementSystem.DTOs.ProductionOrders
{
    public class ProductionOrderDto
    {
        [JsonPropertyName("productionOrderId")]
        public int ProductionOrderId { get; set; }

        [JsonPropertyName("productionLine")]
        public string? ProductionLine { get; set; }

        [JsonPropertyName("productCode")]
        public string? ProductCode { get; set; }

        [JsonPropertyName("productName")]
        public string? ProductName { get; set; }

        [JsonPropertyName("productionOrderNumber")]
        [Required]
        [MinLength(1)]
        public string ProductionOrderNumber { get; set; } = null!;

        [JsonPropertyName("recipeCode")]
        [Required]
        [MinLength(1)]
        public string RecipeCode { get; set; } = null!;

        [JsonPropertyName("recipeName")]
        public string? RecipeName { get; set; }

        [JsonPropertyName("recipeVersion")]
        public string RecipeVersion { get; set; } = null!;

        [JsonPropertyName("shift")]
        public string? Shift { get; set; }

        [JsonPropertyName("plannedStart")]
        public DateTime? PlannedStart { get; set; }

        [JsonPropertyName("plannedEnd")]
        public DateTime? PlannedEnd { get; set; }

        [JsonPropertyName("quantity")]
        public decimal? Quantity { get; set; }

        [JsonPropertyName("unitOfMeasurement")]
        public string? UnitOfMeasurement { get; set; }

        [JsonPropertyName("lotNumber")]
        public string? LotNumber { get; set; }

        [JsonPropertyName("processArea")]
        public string? ProcessArea { get; set; }

        [JsonPropertyName("status")]
        public int? Status { get; set; }

        [JsonPropertyName("currentBatch")]
        public string? CurrentBatch { get; set; }

        [JsonPropertyName("totalBatches")]
        public int TotalBatches { get; set; }

        [JsonPropertyName("recipeDetailsId")]
        public int? RecipeDetailsId { get; set; }

        [JsonPropertyName("productQuantity")]
        public decimal? ProductQuantity { get; set; }

        [JsonPropertyName("batches")]
        public IEnumerable<BatchDto> Batches { get; set; } = new List<BatchDto>();
    }

    public class OrderStatsDto
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("inProgress")]
        public int InProgress { get; set; }

        [JsonPropertyName("completed")]
        public int Completed { get; set; }

        [JsonPropertyName("stopped")]
        public int Stopped { get; set; }
    }

    public class OrderFiltersDto
    {
        [JsonPropertyName("processAreas")]
        public IEnumerable<string> ProcessAreas { get; set; } = new List<string>();

        [JsonPropertyName("shifts")]
        public IEnumerable<string> Shifts { get; set; } = new List<string>();

        [JsonPropertyName("productionOrderNumbers")]
        public IEnumerable<string> ProductionOrderNumbers { get; set; } = new List<string>();
    }
}
