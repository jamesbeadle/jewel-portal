using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Ingestion;

/// <summary>
/// The completeness safety net. Periodically walks the Inbox /messages/delta feed from a durable
/// cursor and ingests anything new. On the very first run (no cursor) this performs the one-time
/// backlog import — paging the whole Inbox to seed IntakeEmails — then persists the deltaLink as
/// the ongoing cursor. Thereafter each run only sees changes since the last cursor.
///
/// The webhook (when enabled) provides speed; this sweep provides completeness, so a missed or
/// expired webhook can never lose a message.
/// </summary>
public sealed class MailboxDeltaSweep
{
    private readonly MailboxIntakeOptions _options;
    private readonly IGraphMailClient _graph;
    private readonly MailboxSyncStateStore _store;
    private readonly IntakeIngestionService _ingestion;
    private readonly ILogger<MailboxDeltaSweep> _logger;

    public MailboxDeltaSweep(
        MailboxIntakeOptions options,
        IGraphMailClient graph,
        MailboxSyncStateStore store,
        IntakeIngestionService ingestion,
        ILogger<MailboxDeltaSweep> logger)
    {
        _options = options;
        _graph = graph;
        _store = store;
        _ingestion = ingestion;
        _logger = logger;
    }

    [Function(nameof(MailboxDeltaSweep))]
    public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo timer, CancellationToken ct)
    {
        if (!_options.Enabled || !_options.EnableDeltaSweep || !_options.IsConfigured)
            return;

        var state = await _store.GetOrCreateAsync(ct);
        var firstRun = string.IsNullOrEmpty(state.DeltaLink);

        if (firstRun)
            _logger.LogInformation("Mailbox delta sweep: no cursor yet — running one-time backlog import.");

        var link = state.DeltaLink; // null => fresh delta enumeration (backlog import)
        var ingested = 0;
        string? deltaLink = null;

        do
        {
            var page = await _graph.GetDeltaPageAsync(link, ct);
            foreach (var message in page.Messages)
            {
                if (await _ingestion.IngestAsync(message, ct))
                    ingested++;
            }

            link = page.NextLink;
            deltaLink = page.DeltaLink ?? deltaLink;
        }
        while (!string.IsNullOrEmpty(link));

        if (!string.IsNullOrEmpty(deltaLink))
            state.DeltaLink = deltaLink;

        state.LastSyncedAt = DateTimeOffset.UtcNow;
        if (firstRun)
            state.BacklogImported = true;

        await _store.SaveAsync(ct);

        if (ingested > 0)
            _logger.LogInformation("Mailbox delta sweep ingested {Count} new email(s).", ingested);
    }
}
