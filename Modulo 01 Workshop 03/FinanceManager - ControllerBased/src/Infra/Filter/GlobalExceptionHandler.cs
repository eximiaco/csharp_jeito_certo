using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FinanceManager.Infra.Filter;

public class GlobalExceptionHandler(IWebHostEnvironment env, Serilog.ILogger logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        logger.Fatal(context.Exception.Message, context.Exception.Message);

        var json = new JsonErrorResponse
        {
            Messages = new[] { "An error occur.Try it again." }
        };

        if (env.IsDevelopment())
        {
            json.DeveloperMessage = context.Exception.ToString();
        }
           
        context.Result = new InternalServerErrorObjectResult(json);
        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
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