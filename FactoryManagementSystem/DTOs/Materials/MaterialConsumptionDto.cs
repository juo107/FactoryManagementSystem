using System;
using System.Text.Json.Serialization;

namespace FactoryManagementSystem.DTOs.Materials
{
    public class MaterialConsumptionDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("productionOrderNumber")]
        public string? ProductionOrderNumber { get; set; }

        [JsonPropertyName("batchCode")]
        public string? BatchCode { get; set; }

        [JsonPropertyName("quantity")]
        public string? Quantity { get; set; }

        [JsonPropertyName("ingredientCode")]
        public string? IngredientCode { get; set; }

        [JsonPropertyName("ingredientName")]
        public string? IngredientName { get; set; }

        [JsonPropertyName("lot")]
        public string? Lot { get; set; }

        [JsonPropertyName("unitOfMeasurement")]
        public string? UnitOfMeasurement { get; set; }

        [JsonPropertyName("datetime")]
        public DateTime? Datetime { get; set; }

        [JsonPropertyName("operatorId")]
        public string? Operator_ID { get; set; }

        [JsonPropertyName("supplyMachine")]
        public string? SupplyMachine { get; set; }

        [JsonPropertyName("response")]
        public string? Respone { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("status1")]
        public string? Status1 { get; set; }

        [JsonPropertyName("request")]
        public string? Request { get; set; }

        [JsonPropertyName("count")]
        public int? Count { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }
        
        // Joined fields from ProductionOrders
        [JsonPropertyName("shift")]
        public string? Shift { get; set; }

        [JsonPropertyName("productionLine")]
        public string? ProductionLine { get; set; }
    }

    // DTO nhận dữ liệu lọc từ Frontend
    public class BatchFilterDto
    {
        [JsonPropertyName("batchCode")]
        public string? BatchCode { get; set; }
    }

    public class MaterialConsumptionResponseDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("productionOrderNumber")]
        public string? ProductionOrderNumber { get; set; }

        [JsonPropertyName("batchCode")]
        public string? BatchCode { get; set; }

        [JsonPropertyName("ingredientCode")]
        public string? IngredientCode { get; set; }

        [JsonPropertyName("ingredientName")]
        public string? IngredientName { get; set; }

        [JsonPropertyName("lot")]
        public string? Lot { get; set; }

        [JsonPropertyName("quantity")]
        public decimal? Quantity { get; set; }

        [JsonPropertyName("unitOfMeasurement")]
        public string? UnitOfMeasurement { get; set; }

        [JsonPropertyName("datetime")]
        public DateTime? Datetime { get; set; }

        [JsonPropertyName("operatorId")]
        public string? Operator_ID { get; set; }

        [JsonPropertyName("supplyMachine")]
        public string? SupplyMachine { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("request")]
        public string? Request { get; set; }

        [JsonPropertyName("response")]
        public string? Respone { get; set; }

        [JsonPropertyName("status1")]
        public string? Status1 { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }
    }
}
