using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WebApp;

public static class PredictionsClient
{
    private const string MyPredictionsUrl = "https://babg2026fifa.ddns.net/api/my-predictions";
    private const string PredictionsUrl = "https://babg2026fifa.ddns.net/api/predictions";

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<PredictionSubmissionWorkflowResult> SubmitAsync(
        IHttpClientFactory httpClientFactory,
        PredictionSubmissionModel model,
        CredentialsModel credentials)
    {
        var client = httpClientFactory.CreateClient();
        var results = new List<PredictionSubmitResult>();
        var existingPredictions = new JsonObject();

        foreach (var item in credentials.Items)
        {
            var myPredictions = await GetMyPredictionsAsync(client, item, model.Round);
            if (myPredictions is null)
            {
                continue;
            }

            existingPredictions[item.Email] = myPredictions;

            using var request = new HttpRequestMessage(HttpMethod.Post, PredictionsUrl);
            AddHeaderIfConfigured(request, "X-User-Email", item.Email);
            AddHeaderIfConfigured(request, "X-User-Passcode", item.Code);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { round = model.Round, picks = model.PredictionPicks }),
                Encoding.UTF8,
                "application/json");

            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            results.Add(new PredictionSubmitResult(item.Email, ParseResponseBody(responseBody)));
        }

        var existingPredictionsJson = existingPredictions.Count == 0
            ? JsonSerializer.Serialize(new
            {
                message = "沒有取得已有預測內容的 credential。",
            })
            : JsonSerializer.Serialize(existingPredictions);

        if (results.Count == 0)
        {
            var json = JsonSerializer.Serialize(new
            {
                message = "沒有符合送出條件的 credential。my-predictions 回傳 null 的帳號已跳過。",
            });

            return new PredictionSubmissionWorkflowResult(existingPredictionsJson, json);
        }

        return new PredictionSubmissionWorkflowResult(existingPredictionsJson, JsonSerializer.Serialize(results));
    }

    public sealed record PredictionSubmissionWorkflowResult(string ExistingPredictionsJson, string SubmissionResultsJson);

    private static async Task<JsonNode?> GetMyPredictionsAsync(HttpClient client, CredentialItem item, int round)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{MyPredictionsUrl}?round={round}");
        AddHeaderIfConfigured(request, "X-User-Email", item.Email);
        AddHeaderIfConfigured(request, "X-User-Passcode", item.Code);

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        return ParseMyPredictionsResponseBody(responseBody);
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
        if (IsNullResponse(responseBody))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(responseBody);
        }
        catch (JsonException)
        {
            return JsonValue.Create(responseBody);
        }
    }

    private static JsonNode? ParseMyPredictionsResponseBody(string responseBody)
    {
        if (IsNullResponse(responseBody))
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
            return ParseResponseBody(responseBody);
        }
    }

    private static bool IsNullResponse(string responseBody)
    {
        return string.IsNullOrWhiteSpace(responseBody) || responseBody.Trim() == "null";
    }

    private sealed record PredictionSubmitResult(string Email, JsonNode? Result);
}
