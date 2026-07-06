using System.Threading.Channels;

namespace WebApp;

public sealed class BackgroundPredictionSubmissionService(
    IHttpClientFactory httpClientFactory,
    ILogger<BackgroundPredictionSubmissionService> logger) : BackgroundService
{
    private readonly Channel<BackgroundPredictionSubmissionJob> _jobs = Channel.CreateUnbounded<BackgroundPredictionSubmissionJob>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
        });

    public async ValueTask<Guid> QueueAsync(PredictionSubmissionModel model, CredentialsModel credentials)
    {
        var job = new BackgroundPredictionSubmissionJob(
            Guid.NewGuid(),
            CloneModel(model),
            CloneCredentials(credentials));

        await _jobs.Writer.WriteAsync(job);
        logger.LogInformation(
            "Queued prediction submission job {JobId} with {CredentialCount} credentials for round {Round}.",
            job.Id,
            job.Credentials.Items.Length,
            job.Model.Round);

        return job.Id;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _jobs.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessAsync(job, stoppingToken);
        }
    }

    private static CredentialsModel CloneCredentials(CredentialsModel credentials)
    {
        return new CredentialsModel(
            credentials.Items
                .Select(item => new CredentialItem(item.Email, item.Code))
                .ToArray());
    }

    private static PredictionSubmissionModel CloneModel(PredictionSubmissionModel model)
    {
        return new PredictionSubmissionModel
        {
            Round = model.Round,
            PredictionPicks = model.PredictionPicks.ToArray(),
        };
    }

    private async Task ProcessAsync(BackgroundPredictionSubmissionJob job, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Started prediction submission job {JobId}.", job.Id);
            var result = await PredictionsClient.SubmitAsync(
                httpClientFactory,
                job.Model,
                job.Credentials,
                cancellationToken);

            logger.LogInformation(
                "Completed prediction submission job {JobId}. Existing predictions: {ExistingPredictions}. Submission results: {SubmissionResults}.",
                job.Id,
                result.ExistingPredictionsJson,
                result.SubmissionResultsJson);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Canceled prediction submission job {JobId} during shutdown.", job.Id);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed prediction submission job {JobId}.", job.Id);
        }
    }

    private sealed record BackgroundPredictionSubmissionJob(
        Guid Id,
        PredictionSubmissionModel Model,
        CredentialsModel Credentials);
}
