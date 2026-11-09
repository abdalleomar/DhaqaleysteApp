namespace DhaqaleysteApp.Services;

/// <summary>The outcome of an API call: either data or a message that can be shown to the user.</summary>
public class ApiResult
{
    public bool Success { get; init; }
    public string Error { get; init; } = "";
    public bool IsNetworkError { get; init; }
    public bool IsUnauthorized { get; init; }

    public static ApiResult Ok() => new() { Success = true };
    public static ApiResult Fail(string error, bool network = false, bool unauthorized = false) =>
        new() { Error = error, IsNetworkError = network, IsUnauthorized = unauthorized };
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }

    public static ApiResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static new ApiResult<T> Fail(string error, bool network = false, bool unauthorized = false) =>
        new() { Error = error, IsNetworkError = network, IsUnauthorized = unauthorized };
}
