using System.Text.Json.Serialization;

namespace FactoryManagementSystem.DTOs.Recipes
{
    public class ProcessDto
    {
        [JsonPropertyName("processId")]
        public long ProcessId { get; set; }

        [JsonPropertyName("processCode")]
        public string? ProcessCode { get; set; }

        [JsonPropertyName("processName")]
        public string? ProcessName { get; set; }

        [JsonPropertyName("duration")]
        public double? Duration { get; set; }

        [JsonPropertyName("durationUoM")]
        public string? DurationUoM { get; set; }
    }
}
