using System.Text.Json.Serialization;

namespace WebApp;

public sealed class MyPredictions
{
    [JsonPropertyName("_id")]
    public required string Id { get; init; }

    [JsonPropertyName("teamId")]
    public required string TeamId { get; init; }

    [JsonRequired]
    [JsonPropertyName("round")]
    public int Round { get; init; }

    [JsonRequired]
    [JsonPropertyName("stageIdx")]
    public int StageIdx { get; init; }

    [JsonPropertyName("picks")]
    public required string[] Picks { get; init; }

    [JsonRequired]
    [JsonPropertyName("submittedAt")]
    public DateTimeOffset SubmittedAt { get; init; }
}
