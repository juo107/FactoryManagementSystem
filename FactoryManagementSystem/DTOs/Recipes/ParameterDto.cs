using System.Text.Json.Serialization;

namespace FactoryManagementSystem.DTOs.Recipes
{
    public class ParameterDto
    {
        [JsonPropertyName("processId")]
        public long ProcessId { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("parameterName")]
        public string? ParameterName { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }
}
