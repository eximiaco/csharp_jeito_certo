namespace Eximia.Integrations.Routing;

public enum EDispactcherResult
{
    Success,
    Retry,
    Failure
}

public record DispatcherResult
{
    public DispatcherResult(EDispactcherResult result, string message, int statusCode, string body)
    {
        Result = result;
        Message = message;
        StatusCode = statusCode;
        Body = body;
    }
    public static DispatcherResult Success(int statusCode, string body) => new DispatcherResult(EDispactcherResult.Success, string.Empty, statusCode, body);
    public static DispatcherResult DefaultSuccess => new DispatcherResult(EDispactcherResult.Success, string.Empty, 200, "{}");
    public static DispatcherResult Retry(string message, int statusCode, string body) => new DispatcherResult(EDispactcherResult.Retry, message, statusCode, body);
    public static DispatcherResult Failure(string message) => new DispatcherResult(EDispactcherResult.Failure, message, 500, "{}");
    public static DispatcherResult Failure(string message, int statusCode, string body) => new DispatcherResult(EDispactcherResult.Failure, message, statusCode, body);

    public EDispactcherResult Result { get; }
    public string Message { get; }
    public int StatusCode { get; }
    public string Body { get; }
    public bool IsRetry => Result == EDispactcherResult.Retry;
    public bool IsSuccess => Result == EDispactcherResult.Success;
    public bool IsFailure => Result == EDispactcherResult.Failure;
    public bool Is200Family => StatusCode.ToString().Substring(0, 1) == "2";
    public bool IsErrorButNot404 => IsFailure && StatusCode != 404;
}