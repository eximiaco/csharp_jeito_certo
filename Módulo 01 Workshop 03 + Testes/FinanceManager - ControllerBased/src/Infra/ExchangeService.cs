using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Polly;

namespace FinanceManager.Infra;

public class ExchangeServiceOptions
{
    public string ApiUrl { get; set; }
    public int DefaultTimeoutMs { get; set; } = 500;
};

public record CotationResponse(string high);

public class ExchangeService(
    IOptionsMonitor<ExchangeServiceOptions> options,
    ILogger<ExchangeService> logger,
    [FromKeyedServices("circuit-breaker")] ResiliencePipeline circuitBreakerPipeline)
{
    private readonly IOptionsMonitor<ExchangeServiceOptions> _options = options;

    public async Task<CommandResult<decimal>> GetUsdValueAsync(decimal referenceValue)
    {
        logger.LogInformation($"{_options.CurrentValue.DefaultTimeoutMs}");

        var response = await circuitBreakerPipeline.ExecuteAsync(async (cancellationToken) =>
        {
            return await _options.CurrentValue.ApiUrl
                      .WithTimeout(TimeSpan.FromMilliseconds(_options.CurrentValue.DefaultTimeoutMs))
                      .AppendPathSegment("json/last/USD")
                      .AllowAnyHttpStatus()
                      .GetAsync(cancellationToken: cancellationToken);
        });

        if (!response.ResponseMessage.IsSuccessStatusCode)
        {
            var responseContent = await response.GetStringAsync();
            var error = ValidationErrors.General.UnknownError(responseContent);

            return new CommandResult<decimal>(error, CommandResultStatus.Unknown);
        }

        var responseAsJson = await response.GetJsonAsync<Dictionary<string, CotationResponse>>();
        var cotationAsNumber = decimal.Parse(responseAsJson.First().Value.high);

        return referenceValue / cotationAsNumber;
    }
}
