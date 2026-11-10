using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Eximia.Integrations.Shared.OpenTelemetry;
using ILogger = Serilog.ILogger;

namespace Eximia.Integration.BackgroundAgent.Infrastructure;

public class HttpGlobalExceptionFilter : IExceptionFilter
{
    private readonly IWebHostEnvironment _env;
    private readonly ITelemetryFactory _telemetryFactory;
    private readonly ILogger _logger;

    public HttpGlobalExceptionFilter(IWebHostEnvironment env, ITelemetryFactory telemetryFactory, ILogger logger)
    {
        this._env = env;
        _telemetryFactory = telemetryFactory;
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var activity = _telemetryFactory.GetCurrentActivity();
        activity.AddExceptionAndEvent(context.Exception.Message, context.Exception);
        _logger.Fatal(context.Exception, context.Exception.Message);

        var json = new JsonErrorResponse
        {
            Messages = ["An error occur.Try it again."]
        };

        if (_env.IsDevelopment())
        {
            json.DeveloperMessage = context.Exception.ToString();
        }
           
        context.Result = new InternalServerErrorObjectResult(json);
        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        //context.ExceptionHandled = true;
    }

    private class JsonErrorResponse
    {
        public string[]? Messages { get; set; }

        public object? DeveloperMessage { get; set; }
    }

    private class InternalServerErrorObjectResult : ObjectResult
    {
        public InternalServerErrorObjectResult(object error)
            : base(error)
        {
            StatusCode = StatusCodes.Status500InternalServerError;
        }
    }
}
