using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Eximia.Integrations.Shared.Extensions;
using Eximia.Integrations.HttpApi.Models;
using Eximia.Integrations.Shared;
using ILogger = Serilog.ILogger;

namespace Eximia.Integrations.HttpApi.Infrastructure;

public sealed class JsonNodeInputFormatter : InputFormatter
{
    private readonly ILogger _logger;

    public JsonNodeInputFormatter(ILogger logger)
    {
        _logger = logger;
        SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
    }

    public override bool CanRead(InputFormatterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var contentType = context.HttpContext.Request.ContentType;
        return contentType == "application/json" || contentType == "application/json; charset=UTF-8";
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
    {
        var request = context.HttpContext.Request;
        try
        {
            using var reader = new StreamReader(request.Body);
            var content = await reader.ReadToEndAsync();
            var jsonNode = JsonNode.Parse(content);
            if (!jsonNode!.AsObject().TryGetValue("metadata", out var meta, out var exception))
                return await InputFormatterResult.SuccessAsync(JsonNode.Parse(content));

            var metaJson = meta!.ToJsonString();
            var metadata = metaJson.ToObject<Metadata>();

            if (context.HttpContext.Request.Headers.TryGetValue("x-eximia-integration-event-id", out var eventId))
                metadata.IntegrationEventId = int.TryParse(eventId, out var id) ? id : null;
            return await InputFormatterResult.SuccessAsync(new IntegrationEventRequest(metadata, jsonNode));
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error formatting request body");
            return await InputFormatterResult.FailureAsync();
        }
    }
}
