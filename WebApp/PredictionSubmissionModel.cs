using System.Text.Json.Serialization;

namespace WebApp;

public sealed class PredictionSubmissionModel
{
    public const int RandomPickCount = 8;

    [JsonRequired]
    [JsonPropertyName("round")]
    public int Round { get; init; }

    [JsonPropertyName("predictionPicks")]
    public required string[] PredictionPicks { get; init; }
}
