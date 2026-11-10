using CSharpFunctionalExtensions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Files.Services;
using Eximia.Integrations.Routing;

namespace Eximia.Integrations.IntegrationEvents.Services;

public class PayloadService(
    UploadFileService uploadFileService,
    IConfiguration configuration)
{
    private readonly string _bucketName = configuration.GetValue<string>("GoogleStorage:bucket_name")!;

    public async Task<UploadPayloadResponse> UploadAsync(string payload, Guid fileId, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;

        var uploadResponse = await uploadFileService.UploadJsonToEventsAsync(
            _bucketName,
            payload,
            $"eventos/{now.Year}/{now.Month}/{now.Day}/{fileId}.json",
            cancellationToken);

        var newPayload = payload.ToObject<IntegrationEventDto>(false);
        newPayload.Data = new
        {
            fileName = uploadResponse.FileName
        };
        return new UploadPayloadResponse(uploadResponse.FileName, newPayload.ToJson());
    }

    public async Task UploadAsync(IntegrationEvent integrationEvent, string payload, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        if (integrationEvent.PayloadFileId == Guid.Empty)
            integrationEvent.PayloadFileId = Guid.NewGuid();

        var uploadResponse = await uploadFileService.UploadJsonToEventsAsync(
            _bucketName,
            payload,
            $"eventos/{now.Year}/{now.Month}/{now.Day}/{integrationEvent.PayloadFileId}.json",
            cancellationToken);

        var newPayload = payload.ToObject<IntegrationEventDto>(false);
        newPayload.Data = new
        {
            fileName = uploadResponse.FileName
        };

        integrationEvent.IsPayloadLink = true;
        integrationEvent.PayloadFile = uploadResponse.FileName;
    }

    public async Task<Result<string>> DownloadAsync(
        IntegrationEvent integrationEvent,
        bool sendPayloadAsLink,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(integrationEvent.PayloadFile))
            return Result.Failure<string>("Payload not found.");

        var extension = Path.GetExtension(integrationEvent.PayloadFile);
        if (string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase) || sendPayloadAsLink)
            return integrationEvent.BuildPayload();

        return await uploadFileService.DownloadAsStringAsync(integrationEvent.PayloadFile, cancellationToken);
    }

    public record IntegrationEventDto
    {
        [JsonProperty("metadata")] public object Metadata { get; set; } = null!;

        [JsonProperty("data")] public object Data { get; set; } = null!;
    }
}

public record UploadPayloadResponse(string FileName, string NewPayload);
