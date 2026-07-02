using System.Text.Json.Serialization;

namespace WebApp;

public sealed class PredictionSubmissionModel
{
    [JsonRequired]
    [JsonPropertyName("round")]
    public int Round { get; init; }

    [JsonPropertyName("predictionPicks")]
    public required string[] PredictionPicks { get; init; }
}
