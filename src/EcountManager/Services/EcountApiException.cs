namespace EcountManager.Services;

public sealed class EcountApiException(string message, string? errorCode = null) : Exception(message)
{
    public string? ErrorCode { get; } = errorCode;
}

