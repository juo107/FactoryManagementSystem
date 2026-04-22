using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FactoryManagementSystem.DTOs.Products
{
    public class ProductDto
    {
        [JsonPropertyName("productMasterId")]
        public int ProductMasterId { get; set; }

        [JsonPropertyName("itemCode")]
        [Required]
        [MinLength(1)]
        public string ItemCode { get; set; } = null!;

        [JsonPropertyName("itemName")]
        [Required]
        [MinLength(1)]
        public string ItemName { get; set; } = null!;

        [JsonPropertyName("itemType")]
        public string? Item_Type { get; set; }

        [JsonPropertyName("group")]
        public string? Group { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("brand")]
        public string? Brand { get; set; }

        [JsonPropertyName("baseUnit")]
        public string? BaseUnit { get; set; }

        [JsonPropertyName("inventoryUnit")]
        public string? InventoryUnit { get; set; }

        [JsonPropertyName("itemStatus")]
        public string? Item_Status { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }

        [JsonPropertyName("mhuTypes")]
        public List<MhuTypeDto> MhuTypes { get; set; } = new();

        // Internal property for Dapper auto-mapping - Hidden from JSON by default or keep it internal
        [JsonIgnore]
        public string? MhuTypesJson { get; set; }
    }

    public class MhuTypeDto
    {
        [JsonPropertyName("mhuTypeId")]
        public int MHUTypeId { get; set; }

        [JsonPropertyName("fromUnit")]
        public string? FromUnit { get; set; }

        [JsonPropertyName("toUnit")]
        public string? ToUnit { get; set; }

        [JsonPropertyName("conversion")]
        public decimal Conversion { get; set; }
    }
}
