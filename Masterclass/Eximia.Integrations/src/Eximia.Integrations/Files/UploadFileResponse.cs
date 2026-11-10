namespace Eximia.Integrations.Files
{
    public record UploadFileResponse(
        string BucketName,
        long? Generation,
        string Id,
        string FileName,
        string Uri);
}
