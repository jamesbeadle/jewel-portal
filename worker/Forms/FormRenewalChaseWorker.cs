using Jewel.JPMS.Api.Features.Forms.Retention;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Worker.Forms;

/// <summary>The renewal chase each morning: insurance and tickets asked for before they lapse, each with its form as a one-time link.</summary>
public sealed class FormRenewalChaseWorker
{
    private readonly FormRenewalChase chase;
    private readonly ILogger<FormRenewalChaseWorker> logger;

    public FormRenewalChaseWorker(FormRenewalChase chase, ILogger<FormRenewalChaseWorker> logger)
    {
        this.chase = chase;
        this.logger = logger;
    }

    [Function(nameof(FormRenewalChaseWorker))]
    public async Task Run([TimerTrigger("0 30 7 * * *")] TimerInfo timer, CancellationToken cancellationToken)
    {
        var outcome = await chase.RunAsync(DateTimeOffset.UtcNow, cancellationToken);
        logger.LogInformation("Renewal chase: {Insurance} insurance renewal(s) and {Training} ticket renewal(s) asked for.",
            outcome.Insurance, outcome.Training);
    }
}
