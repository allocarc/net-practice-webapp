using System.Text.Json;
using System.Text.Json.Nodes;

namespace WebApp;

public static class MyPredictionsClient
{
    private const string MyPredictionsUrl = "https://babg2026fifa.ddns.net/api/my-predictions";

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<string> GetAsync(IHttpClientFactory httpClientFactory, int round, CredentialsModel credentials)
    {
        var client = httpClientFactory.CreateClient();
        var nullEmails = new JsonArray();

        foreach (var item in credentials.Items)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{MyPredictionsUrl}?round={round}");
            AddHeaderIfConfigured(request, "X-User-Email", item.Email);
            AddHeaderIfConfigured(request, "X-User-Passcode", item.Code);

            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            var result = ParseResponseBody(responseBody);

            if (result is null)
            {
                nullEmails.Add(item.Email);
            }
        }

        var output = new JsonObject
        {
            ["nullEmails"] = nullEmails,
        };

        return JsonSerializer.Serialize(output);
    }

    private static void AddHeaderIfConfigured(HttpRequestMessage request, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        request.Headers.TryAddWithoutValidation(name, value);
    }

    private static JsonNode? ParseResponseBody(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody) || responseBody.Trim() == "null")
        {
            return null;
        }

        try
        {
            var prediction = JsonSerializer.Deserialize<MyPredictions>(responseBody, DeserializeOptions);
            return JsonSerializer.SerializeToNode(prediction);
        }
        catch (JsonException)
        {
            return ParseRawResponseBody(responseBody);
        }
    }

    private static JsonNode? ParseRawResponseBody(string responseBody)
    {
        try
        {
            return JsonNode.Parse(responseBody);
        }
        catch (JsonException)
        {
            return JsonValue.Create(responseBody);
        }
    }
}
