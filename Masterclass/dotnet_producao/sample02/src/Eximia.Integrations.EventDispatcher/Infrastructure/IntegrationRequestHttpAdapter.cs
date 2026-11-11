using Eximia.Integrations.Shared;

namespace Eximia.Integrations.EventDispatcher.Infrastructure;

public sealed class IntegrationRequestHttpAdapter : IIntegrationRequest
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IntegrationRequestHttpAdapter(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string Origin 
    {
        get
        {
            _httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("x-eximia-source",  out var origin);
            return origin;
        }
    }
    public string OriginKey 
    {
        get
        {
            _httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("x-eximia-source-key",  out var origin);
            return origin;
        }
    }
    public string DestinyKey 
    {
        get
        {
            _httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("x-eximia-destiny-key",  out var id);
            return id;
        }
    }
    public string CorrelationId {
        get
        {
            _httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("x-eximia-correlation-id",  out var id);
            return id;
        }
    }
    
    public int SchemaVersion {
        get
        {
            _httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("x-eximia-schema-version",  out var id);
            return string.IsNullOrEmpty(id) ? 0 : int.Parse(id);
        }
    }

    public bool IsCallBack { 
        get
        {
            _httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("x-eximia-is-callback",  out var id);
            return !string.IsNullOrEmpty(id) && bool.Parse(id);
        } 
    }
}
