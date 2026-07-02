namespace WebApp;

public static class FifaResultsClient
{
    private const string ResultsUrl = "https://babg2026fifa.ddns.net/api/results";

    public static async Task<string> GetResultsAsync(IHttpClientFactory httpClientFactory)
    {
        var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync(ResultsUrl);

        return await response.Content.ReadAsStringAsync();
    }
}
