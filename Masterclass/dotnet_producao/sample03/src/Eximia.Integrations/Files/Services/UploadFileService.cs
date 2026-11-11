using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using System.Text;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.Files.Services;

public class UploadFileService(
    StorageClient storageClient,
    UrlSigner urlSigner,
    IConfiguration configuration)
{
    private readonly string _bucketName = configuration.GetValue<string>("GoogleStorage:bucket_name")!;

    public async Task<UploadFileResponse> UploadAsync(
        string bucketName,
        MemoryStream file,
        string fileName,
        string contentType,
        CancellationToken cancellationToken,
        bool generateUrl = false)
    {
        var uploadedFile = await storageClient.UploadObjectAsync(
            bucketName,
            fileName,
            contentType,
            file,
            cancellationToken: cancellationToken);

        var url = string.Empty;
        if (generateUrl)
            url = await GenerateReadSignedUrlAsync(fileName, cancellationToken, bucketName);

        return new UploadFileResponse(
            uploadedFile.Bucket,
            uploadedFile.Generation,
            uploadedFile.Id,
            uploadedFile.Name,
            url);
    }

    public async Task<UploadFileResponse> UploadJsonToEventsAsync(
        string bucketName,
        string payload,
        string fileName,
        CancellationToken cancellationToken,
        bool generateUrl = false)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        await using var stream = new MemoryStream(bytes);
        return await UploadAsync(bucketName, stream, fileName, "application/json", cancellationToken, generateUrl);
    }

    public async Task<string> GenerateReadSignedUrlAsync(
        string objectPath,
        CancellationToken cancellationToken,
        string bucketName = null!)
    {
        bucketName ??= _bucketName;

        return await urlSigner.SignAsync(
            bucketName,
            objectPath,
            TimeSpan.FromDays(7),
            HttpMethod.Get,
            cancellationToken: cancellationToken);
    }

    public async Task<string> DownloadAsStringAsync(
        string objectPath,
        CancellationToken cancellationToken,
        string bucketName = null!)
    {
        bucketName ??= _bucketName;

        await using var memoryStream = new MemoryStream();
        await storageClient.DownloadObjectAsync(
            bucketName,
            objectPath,
            memoryStream,
            cancellationToken: cancellationToken);

        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
