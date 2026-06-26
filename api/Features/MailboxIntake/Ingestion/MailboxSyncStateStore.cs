using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Ingestion;

/// <summary>
/// Reads/writes the single durable sync-state row for the mailbox (delta cursor, backlog flag,
/// current subscription). The row is created on first use.
/// </summary>
public sealed class MailboxSyncStateStore
{
    private readonly JpmsContext _context;
    private readonly MailboxIntakeOptions _options;

    public MailboxSyncStateStore(JpmsContext context, MailboxIntakeOptions options)
    {
        _context = context;
        _options = options;
    }

    public async Task<MailboxSyncStateEntity> GetOrCreateAsync(CancellationToken ct)
    {
        var state = await _context.MailboxSyncStates
            .FirstOrDefaultAsync(s => s.Mailbox == _options.Mailbox, ct);

        if (state is null)
        {
            state = new MailboxSyncStateEntity { Mailbox = _options.Mailbox };
            _context.MailboxSyncStates.Add(state);
            await _context.SaveChangesAsync(ct);
        }

        return state;
    }

    public Task SaveAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
