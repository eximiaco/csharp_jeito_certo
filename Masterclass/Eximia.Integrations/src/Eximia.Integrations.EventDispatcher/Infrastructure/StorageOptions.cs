namespace Eximia.Integrations.EventDispatcher.Infrastructure;

public class StorageOptions
{
    public string Type { get; set; } = string.Empty;
    public string Project_id { get; set; } = string.Empty;
    public string Private_key_id { get; set; } = string.Empty;
    public string Private_key { get; set; } = string.Empty;
    public string Client_email { get; set; } = string.Empty;
    public string Client_id { get; set; } = string.Empty;
    public string Auth_uri { get; set; } = string.Empty;
    public string Token_uri { get; set; } = string.Empty;
    public string Auth_provider_x509_cert_url { get; set; } = string.Empty;
    public string Client_x509_cert_url { get; set; } = string.Empty;
    public string Bucket_name { get; set; } = string.Empty;
}
