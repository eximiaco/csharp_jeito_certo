using System.Text;
using CSharpFunctionalExtensions;

namespace Eximia.Integrations.Shared;

public abstract class ResultAggregateCommonLogic
{
    protected ResultAggregateCommonLogic()
    {
        NestedResults = new Dictionary<string,Result>(capacity: 1);
    }
    protected ResultAggregateCommonLogic(Dictionary<string, Result> results)
    {
        NestedResults = results;
    }

    protected ResultAggregateCommonLogic(KeyValuePair<string, Result> result)
    {
        NestedResults = new Dictionary<string, Result>();
        AddResult(result.Key, result.Value);
    }
    
    public Dictionary<string, Result> NestedResults { get; }
    public bool IsFailure => NestedResults.Any(c => c.Value.IsFailure);
    public bool IsSuccess => !this.IsFailure;

    public void AddResult(string instance, Result result)
    {
        NestedResults.Add(instance, result);
    }

    public override string ToString()
    {
        if (IsSuccess) return "Success";
        var error = new StringBuilder();
        foreach (var result in NestedResults)
        {
            error.Append($"[{result.Key}] => {result.Value}");
        }

        return $"Failure : {error.ToString()}";
    }
}


public sealed class ResultAggregate : ResultAggregateCommonLogic
{
    private ResultAggregate() : base(){}
    internal ResultAggregate(Dictionary<string, Result> results) : base(results) { }
    private ResultAggregate(KeyValuePair<string, Result> result) : base(result){ }
    
    public ResultAggregate<K> ConvertFailure<K>()
    {
        if (IsSuccess)
            throw new InvalidOperationException("failed because the Result is in a success state.");

        return Failure<K>(NestedResults);
    }
    
    public static ResultAggregate Success() => new ResultAggregate();
    
    public static ResultAggregate Combine(params ResultAggregate[] results)
    {
        List<ResultAggregate> failedResults = results.Where(x => x.IsFailure).ToList();

        if (failedResults.Count == 0)
            return Success();

        var error = new Dictionary<string, Result>();
        foreach (var result in failedResults.SelectMany(c=> c.NestedResults))
            error.Add(result.Key, result.Value);
        
        return Failure(error);
    }
    
    public static ResultAggregate FailureIf(bool isFailure, KeyValuePair<string, Result> error)
        => SuccessIf(!isFailure, error);
    public static ResultAggregate FailureIf(bool isFailure, string field, string error)
        => SuccessIf(!isFailure, CreateResult(field, error));
    public static ResultAggregate FailureIf(Func<bool> failurePredicate, string field, string error)
        => SuccessIf(!failurePredicate(), CreateResult(field, error));

    private static ResultAggregate SuccessIf(bool isSuccess,  KeyValuePair<string, Result> error)
    {
        return isSuccess
            ? Success()
            : Failure(error);
    }

    internal static ResultAggregate Failure(Dictionary<string, Result> results) 
        => new ResultAggregate(results);
    public static ResultAggregate Failure(KeyValuePair<string, Result> result) 
        => new ResultAggregate(result);
    public static ResultAggregate Failure(string field, string error) 
        => Failure(CreateResult(field, error));
    public static ResultAggregate<T> Failure<T>(Dictionary<string, Result> results)
     => new ResultAggregate<T>(results, default!);
    
    private static KeyValuePair<string, Result> CreateResult(string field, string error)
        => new KeyValuePair<string, Result>(field, Result.Failure(error));
}

public sealed class ResultAggregate<T> : ResultAggregateCommonLogic
{
    public ResultAggregate(Dictionary<string, Result> results, T value) : base(results)
    {
        _value = value;
    }
    private readonly T _value;
    public T Value => IsSuccess ? _value : throw new ResultFailureException();
    
    private static ResultAggregate<T> Failure(Dictionary<string, Result> results) 
        => new ResultAggregate<T>(results, default!);
    
    public static ResultAggregate<T> Success(T value)
    {
        return new ResultAggregate<T>(new Dictionary<string, Result>(), value);
    }
    
    public static implicit operator ResultAggregate<T>(T value)
    {
        if (value is ResultAggregate<T> result)
        {
            T resultValue = (result.IsSuccess ? result.Value : default)!;

            return new ResultAggregate<T>(result.NestedResults, resultValue);
        }

        return ResultAggregate<T>.Success(value);
    }
    
    public static implicit operator ResultAggregate(ResultAggregate<T> result)
    {
        if (result.IsSuccess)
            return ResultAggregate.Success();
        else
            return ResultAggregate.Failure(result.NestedResults);
    }
}

public class ResultFailureException : Exception
{
    internal ResultFailureException()
        : base("You attempted to access the Value property for a failed result. A failed result has no Value.")
    { }
}
