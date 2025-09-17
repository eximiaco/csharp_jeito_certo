namespace FinanceManager.Infra;

public struct Result<T>
{
    public bool Success { get; private set; }
    public T? Value { get; private set; }
    public IEnumerable<ErrorMessage> Errors { get; private set; } = [];

    private Result(T value)
    {
        Success = true;
        Value = value;
    }

    private Result(IEnumerable<ErrorMessage> errors)
    {
        Success = false;
        Errors = errors;
    }

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(IEnumerable<ErrorMessage> errors) => new(errors);
    public static Result<T> Fail(ErrorMessage error) => new([error]);

    public static implicit operator Result<T>(ErrorMessage error) => Fail(error);

    public static implicit operator Result<T>(ErrorMessage[] errors) => Fail(errors);

    public static implicit operator Result<T>(T value) => Ok(value);

    public readonly bool TryGetValue(out T value)
    {
        value = Value!;
        return Success;
    }
}

public struct Result
{
    public bool Success { get; private set; }
    public IEnumerable<ErrorMessage> Errors { get; private set; } = [];

    private Result(bool success)
    {
        Success = success;
    }

    private Result(IEnumerable<ErrorMessage> errors)
    {
        Success = false;
        Errors = errors;
    }

    public static Result Ok() => new(true);
    public static Result Fail(IEnumerable<ErrorMessage> errors) => new(errors);
    public static Result Fail(ErrorMessage error) => new([error]);
}
