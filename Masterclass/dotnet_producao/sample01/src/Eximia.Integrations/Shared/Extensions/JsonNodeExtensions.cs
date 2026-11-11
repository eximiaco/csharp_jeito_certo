using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Eximia.Integrations.Shared.Extensions;

public static class JsonNodeExtensions
{
    public static decimal? GetNumber(this JsonValue value)
    {
        if (value.TryGetValue(out JsonElement e))
        {
            if (e.ValueKind != JsonValueKind.Number) return null;
            return e.GetDecimal();
        }

        var number = GetInteger(value);
        if (number != null) return number;

        if (value.TryGetValue(out float f)) return (decimal)f;
        if (value.TryGetValue(out double d)) return (decimal)d;
        if (value.TryGetValue(out decimal dc)) return dc;

        return null;
    }
    
    public static long? GetInteger(this JsonValue value)
    {
        if (value.TryGetValue(out JsonElement e))
        {
            if (e.ValueKind != JsonValueKind.Number) return null;
            var d = e.GetDecimal();
            if (d == Math.Floor(d)) return (long)d;
            return null;
        }
        if (value.TryGetValue(out byte b)) return b;
        if (value.TryGetValue(out short s)) return s;
        if (value.TryGetValue(out ushort us)) return us;
        if (value.TryGetValue(out int i)) return i;
        if (value.TryGetValue(out ushort ui)) return ui;
        if (value.TryGetValue(out long l)) return l;
        // this doesn't feel right... throw?
        if (value.TryGetValue(out ulong ul)) return (long)ul;

        return null;
    }
    
    public static bool VerifyJsonObject(this JsonObject obj)
    {
        return obj.TryGetValue("_", out _, out var e) || e == null;
    }
    
    public static bool TryGetValue(this JsonObject obj, string propertyName, out JsonNode? node, out Exception? e)
    {
        e = null;
        try
        {
            return obj.TryGetPropertyValue(propertyName, out node);
        }
        catch (ArgumentException ae)
        {
            e = ae;
            node = null;
            return false;
        }
    }
    
    public static string ToStringJson(this JsonNode obj)
    {
        string jsonPayload;
        var jsonOptions = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        if (obj is JsonObject jsonObject)
            jsonPayload = jsonObject.ToJsonString(jsonOptions);
        else
            jsonPayload = obj.ToJsonString(jsonOptions);
        return jsonPayload;
    }

    public static string ToJsonString(this JsonNode node)
    {
        return node.ToJsonString(new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }
}
