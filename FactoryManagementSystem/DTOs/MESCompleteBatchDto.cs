using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FactoryManagementSystem.DTOs
{
    public class MESCompleteBatchDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("productionOrder")]
        public string? ProductionOrder { get; set; }

        [JsonPropertyName("batchNumber")]
        public string? BatchNumber { get; set; }

        [JsonPropertyName("batchSize")]
        public decimal? BatchSize { get; set; }

        [JsonPropertyName("batchUOM")]
        public string? BatchUOM { get; set; }

        [JsonPropertyName("productCode")]
        public string? ProductCode { get; set; }

        [JsonPropertyName("productName")]
        public string? ProductName { get; set; }

        [JsonPropertyName("machineCode")]
        public string? MachineCode { get; set; }

        [JsonPropertyName("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public DateTime? EndTime { get; set; }

        [JsonPropertyName("transferStatus")]
        public string? TransferStatus { get; set; }

        [JsonPropertyName("retryCount")]
        public int? RetryCount { get; set; }

        [JsonPropertyName("nextRetryAt")]
        public DateTime? NextRetryAt { get; set; }

        [JsonPropertyName("processingAt")]
        public DateTime? ProcessingAt { get; set; }

        [JsonPropertyName("requestJson")]
        public string? RequestJson { get; set; }

        [JsonPropertyName("responseContent")]
        public string? ResponseContent { get; set; }

        [JsonPropertyName("sentAt")]
        public DateTime? SentAt { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class MESCompleteBatchSearchParams
    {
        [JsonPropertyName("page")]
        public int Page { get; set; } = 1;

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 20;

        [JsonPropertyName("searchQuery")]
        public string? SearchQuery { get; set; }

        [JsonPropertyName("productionOrder")]
        public string? ProductionOrder { get; set; }

        [JsonPropertyName("batchNumber")]
        public string? BatchNumber { get; set; }

        [JsonPropertyName("machineCode")]
        public string? MachineCode { get; set; }

        [JsonPropertyName("dateFrom")]
        public string? DateFrom { get; set; }

        [JsonPropertyName("dateTo")]
        public string? DateTo { get; set; }

        [JsonPropertyName("transferStatus")]
        public string? TransferStatus { get; set; }
    }

    public class MESCompleteBatchResponse
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("data")]
        public List<MESCompleteBatchDto> Data { get; set; } = new();
    }
}
