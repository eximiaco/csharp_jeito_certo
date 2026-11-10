using System.Diagnostics;
using System.Reflection;
using Serilog;
using Serilog.Context;

namespace Eximia.Integrations.Shared.OpenTelemetry;

public interface IOtelTelemetry : IDisposable
{
    IOtelTelemetry AddTag(string tag, object? value);
    IOtelTelemetry AddEvent(string message);
    IOtelTelemetry AddInformationAndEvent(string message);
    IOtelTelemetry AddExceptionAndEvent(string error, Exception exception);
    IOtelTelemetry AddException(string error, Exception exception);
    void SetSuccess(string messageTemplate);
    void SetError(string messageTemplate);
    IOtelTelemetry AddWarning(string messageTemplate, params object?[]? propertyValues);
    IOtelTelemetry AddError(string messageTemplate, params object?[]? propertyValues);
    void AddInformation(string messageTemplate, params object?[]? propertyValues);
}

public sealed class OtelTelemetry : IOtelTelemetry
{
    private Activity? _activity;
    private readonly ILogger _logger;
    private bool _statusSet = false;
    private IList<IDisposable> _pushProperties = new List<IDisposable>();

    public OtelTelemetry(Activity? activity, ILogger logger)
    {
        _activity = activity;
        _logger = logger;
    }

    public IOtelTelemetry AddTag(string tag, object? value)
    {
        _activity?.SetTag(tag, value);
        _pushProperties.Add(LogContext.PushProperty(tag, value));
        return this;
    }

    public IOtelTelemetry AddInformationAndEvent(string message)
    {
        _activity?.AddEvent(new ActivityEvent(message));
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        _logger.Information(message);
        return this;
    }
    
    public IOtelTelemetry AddEvent(string message)
    {
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        _activity?.AddEvent(new ActivityEvent(message));
        return this;
    }
    
    public IOtelTelemetry AddExceptionAndEvent(string error, Exception exception)
    {
        _activity?.AddEvent(new ActivityEvent("exception", default, new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.StackTrace }
        }));

        return this;
    }

    public IOtelTelemetry AddException(string error, Exception exception)
    {
        AddExceptionAndEvent(error, exception);
        return this;
    }
    
    public void SetSuccess(string messageTemplate)
    {
        _activity?.SetStatus(ActivityStatusCode.Ok, messageTemplate);
        _statusSet = true;
    }

    public void SetError(string messageTemplate)
    {
        _activity?.SetStatus(ActivityStatusCode.Error, messageTemplate);
        _statusSet = true;
    }

    public IOtelTelemetry AddError(string messageTemplate, params object?[]? propertyValues)
    {
        // ReSharper disable twice TemplateIsNotCompileTimeConstantProblem
        if (propertyValues == null || propertyValues.Length== 0) 
            _logger.Error(messageTemplate);
        else
            _logger.Error(messageTemplate, propertyValues?
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(p => p.GetValue(propertyValues))
                .ToArray());
        return this;
    }
    
    public IOtelTelemetry AddWarning(string messageTemplate, params object?[]? propertyValues)
    {
        // ReSharper disable twice TemplateIsNotCompileTimeConstantProblem
        if (propertyValues == null || propertyValues.Length== 0) 
            _logger.Warning(messageTemplate);
        else
            _logger.Warning(messageTemplate, propertyValues?
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(p => p.GetValue(propertyValues))
                .ToArray());
        return this;
    }
    
    public void AddInformation(string messageTemplate, params object?[]? propertyValues)
    {
        // ReSharper disable twice TemplateIsNotCompileTimeConstantProblem
        if (propertyValues == null || propertyValues.Length== 0) 
            _logger.Information(messageTemplate);
        else
            _logger.Information(messageTemplate: messageTemplate, propertyValues);
    }
    
    public void Dispose()
    {
        if(!_statusSet)
            _activity?.SetStatus(ActivityStatusCode.Ok, "Process completed");

        foreach (var pushProperty in _pushProperties)
        {
            pushProperty.Dispose();
        }
        
        _activity?.Dispose();
        _activity = null;
    }
}
