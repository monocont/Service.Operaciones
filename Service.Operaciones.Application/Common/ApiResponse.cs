using System.Text.Json.Serialization;

namespace Service.Operaciones.Application.Common;

public class ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = [];

    [JsonPropertyName("messages")]
    public List<string> Messages { get; set; } = [];

    public static ApiResponse<T> Ok(T data) =>
        new() { Data = data, Success = true };

    public static ApiResponse<T> OkNull() =>
        new() { Data = default, Success = true };

    public static ApiResponse<T> OkNull(List<string> messages) =>
        new() { Data = default, Success = true, Messages = messages };

    public static ApiResponse<T> Ok(T data, List<string> messages) =>
        new() { Data = data, Success = true, Messages = messages };

    public static ApiResponse<T> Fail(List<string> errors) =>
        new() { Success = false, Errors = errors };

    public static ApiResponse<T> Fail(string error) =>
        new() { Success = false, Errors = [error] };
}
