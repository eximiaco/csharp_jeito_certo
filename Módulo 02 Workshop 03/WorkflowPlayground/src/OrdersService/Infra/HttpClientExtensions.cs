namespace OrderService.Infra;

public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> PostWithDelayAsync(
        this HttpClient httpClient,
        string requestUri,
        HttpContent content)
    {
        var delay = new Random().Next(1000, 3000);

        await Task.Delay(delay);

        return await httpClient.PostAsync(requestUri, content);
    }
}