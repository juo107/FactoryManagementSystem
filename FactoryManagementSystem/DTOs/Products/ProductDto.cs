using System;
using System.Collections.Generic;

namespace FactoryManagementSystem.DTOs.Products
{
    public class ProductDto
    {
        public int ProductMasterId { get; set; }
        public string ItemCode { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public string? Item_Type { get; set; }
        public string? Group { get; set; }
        public string? Category { get; set; }
        public string? Brand { get; set; }
        public string? BaseUnit { get; set; }
        public string? InventoryUnit { get; set; }
        public string? Item_Status { get; set; }
        public DateTime? Timestamp { get; set; }
        public List<MhuTypeDto> MhuTypes { get; set; } = new();

        // Internal property for Dapper auto-mapping
        public string? MhuTypesJson { get; set; }
    }

    public class MhuTypeDto
    {
        public int MHUTypeId { get; set; }
        public string? FromUnit { get; set; }
        public string? ToUnit { get; set; }
        public decimal Conversion { get; set; }
    }
}
