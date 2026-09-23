using Jewel.JPMS.Api.Features.Forms;
using Jewel.JPMS.Api.Features.Forms.Retention;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Worker.Forms;

/// <summary>
/// The forms' nightly retention (FormRetention's periods): the destruction is carried out and
/// recorded, not proposed — every form, check, certificate and abandoned upload whose date has come.
/// </summary>
public sealed class FormRetentionWorker
{
    private readonly FormRetentionSweep sweep;
    private readonly ILogger<FormRetentionWorker> logger;

    public FormRetentionWorker(FormRetentionSweep sweep, ILogger<FormRetentionWorker> logger)
    {
        this.sweep = sweep;
        this.logger = logger;
    }

    [Function(nameof(FormRetentionWorker))]
    public async Task Run([TimerTrigger("0 15 4 * * *")] TimerInfo timer, CancellationToken cancellationToken)
    {
        var outcome = await sweep.RunAsync(FormClock.Today(), cancellationToken);
        logger.LogInformation(
            "Forms retention: {Forms} form(s), {Checks} right-to-work check(s), {Certificates} certificate(s), {Uploads} abandoned upload(s) destroyed; {Links} dead link(s) cleared.",
            outcome.Forms, outcome.Checks, outcome.Certificates, outcome.AbandonedUploads, outcome.DeadLinks);
        if (outcome.FilesAlreadyGone > 0)
            logger.LogWarning(
                "Forms retention: {Missing} file(s) were already gone from their store. If that is unexpected, the worker's FormStorage__ConnectionString is not the api's.",
                outcome.FilesAlreadyGone);
    }
}
