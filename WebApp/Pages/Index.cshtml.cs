using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp.Pages;

public class IndexModel(ILogger<IndexModel> logger, IHttpClientFactory httpClientFactory) : PageModel
{
    private const string DefaultCredentialInput = """
        {

        }
        """;

    private const string DefaultPredictionInput = """
        {
          "round": 32,
          "predictionPicks": [
            "BRA",
            "MAR",
            "CAN",
            "PAR",
            "ARG",
            "ESP",
            "FRA",
            "ENG",
            "USA",
            "COL",
            "SUI",
            "MEX",
            "NOR",
            "POR",
            "BEL",
            "EGY"
          ]
        }
        """;

    private readonly ILogger<IndexModel> _logger = logger;

    public string? ExistingPredictionsResult { get; private set; }

    public string? OutputResult { get; private set; }

    [BindProperty]
    public string PredictionInput { get; set; } = DefaultPredictionInput;

    [BindProperty]
    public string CredentialInput { get; set; } = DefaultCredentialInput;

    public void OnGet()
    {

    }

    public async Task OnPostFifaResultsAsync()
    {
        var json = await FifaResultsClient.GetResultsAsync(httpClientFactory);
        OutputResult = FormatJson(json);
    }

    public async Task OnPostSubmitPredictionAsync()
    {
        var credentials = ParseCredentialInput();
        if (credentials is null)
        {
            return;
        }

        var model = ParsePredictionInput();
        if (model is null)
        {
            return;
        }

        var result = await PredictionsClient.SubmitAsync(httpClientFactory, model, credentials);
        ExistingPredictionsResult = FormatJson(result.ExistingPredictionsJson);
        OutputResult = FormatJson(result.SubmissionResultsJson);
    }

    public async Task OnPostGetMyPredictionsAsync()
    {
        var credentials = ParseCredentialInput();
        if (credentials is null)
        {
            return;
        }

        var round = ParseRoundInput();
        if (round is null)
        {
            return;
        }

        var json = await MyPredictionsClient.GetAsync(httpClientFactory, round.Value, credentials);
        OutputResult = FormatJson(json);
    }

    private static readonly JsonSerializerOptions FormatOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly JsonSerializerOptions PredictionInputOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static string FormatJson(string json)
    {
        var parsed = JsonSerializer.Deserialize<JsonElement>(json);
        return JsonSerializer.Serialize(parsed, FormatOptions);
    }

    private PredictionSubmissionModel? ParsePredictionInput()
    {
        try
        {
            var model = JsonSerializer.Deserialize<PredictionSubmissionModel>(PredictionInput, PredictionInputOptions);
            if (model is not null)
            {
                return model;
            }

            OutputResult = "Prediction input is required.";
            return null;
        }
        catch (JsonException exception)
        {
            OutputResult = exception.Message;
            return null;
        }
    }

    private int? ParseRoundInput()
    {
        try
        {
            using var document = JsonDocument.Parse(PredictionInput);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("round", out var roundElement) ||
                !roundElement.TryGetInt32(out var round))
            {
                OutputResult = "Prediction input must include round.";
                return null;
            }

            return round;
        }
        catch (JsonException exception)
        {
            OutputResult = exception.Message;
            return null;
        }
    }

    private CredentialsModel? ParseCredentialInput()
    {
        try
        {
            using var document = JsonDocument.Parse(CredentialInput);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                OutputResult = "Credential input must be a JSON object.";
                return null;
            }

            var items = document.RootElement
                .EnumerateObject()
                .Select(property => new CredentialItem(property.Name, property.Value.ToString()))
                .ToArray();

            return new CredentialsModel(items);
        }
        catch (JsonException exception)
        {
            OutputResult = exception.Message;
            return null;
        }
    }
}
