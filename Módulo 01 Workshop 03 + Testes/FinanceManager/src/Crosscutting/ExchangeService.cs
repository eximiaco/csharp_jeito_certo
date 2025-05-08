using System.ComponentModel.DataAnnotations;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Polly;

namespace FinanceManager.Crosscutting;

public class ExchangeServiceOptions
{
    public static string SectionName => "ExchangeService";

    [Required]
    public string ApiUrl { get; init; } = string.Empty;

    public int DefaultTimeoutMs { get; init; } = 500;
};

public record CotationResponse(string high);

public class ExchangeService(
    IOptionsMonitor<ExchangeServiceOptions> options,
    [FromKeyedServices("circuit-breaker")] ResiliencePipeline circuitBreakerPipeline)
{
    public async Task<Result<decimal>> GetUsdValueAsync(decimal referenceValue)
    {
        var response = await circuitBreakerPipeline.ExecuteAsync(async (cancellationToken) =>
        {
            return await options.CurrentValue.ApiUrl
                .WithTimeout(TimeSpan.FromMilliseconds(options.CurrentValue.DefaultTimeoutMs))
                .AppendPathSegment("json/last/USD")
                .AllowAnyHttpStatus()
                .GetAsync(cancellationToken: cancellationToken);
        });

        if (!response.ResponseMessage.IsSuccessStatusCode)
        {
            var responseContent = await response.GetStringAsync();
            return ValidationErrors.General.UnknownError(responseContent);
        }

        var responseAsJson = await response.GetJsonAsync<Dictionary<string, CotationResponse>>();
        var cotationAsNumber = decimal.Parse(responseAsJson.First().Value.high);

        return referenceValue / cotationAsNumber;
    }
}
