using System;

namespace FactoryManagementSystem.DTOs.Common
{
    public class ApiResponse<T>
    {
        public string Code { get; set; } = "200";
        public string Message { get; set; } = "Success";
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public T? Data { get; set; }

        public static ApiResponse<T> Success(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Code = "200",
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> Error(string message, string code = "500")
        {
            return new ApiResponse<T>
            {
                Code = code,
                Message = message,
                Data = default
            };
        }
    }
}
