using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Todos;

// The bridge from the mailbox compose handler to the to-do timeline: when an email is SENT
// carrying to-do tags (a reply from the item's page inherits its thread's "JPMS/TODO-####" tags;
// a new email from the page is stamped with the item's own), every item those tags name gets an
// "Emailed …" line and, if it was still Open, becomes In progress. That is what lets an assignee
// show "I chased Justine on the 24th" without marking the item done.
//
// Best-effort by design, like the audit trail: the email has already gone, so a failure here is
// logged and swallowed — it must never turn a sent email into an error.
public sealed class TodoEmailActivityRecorder
{
    private readonly JpmsContext context;
    private readonly TodoActivityRecorder activity;
    private readonly ILogger<TodoEmailActivityRecorder> logger;

    public TodoEmailActivityRecorder(JpmsContext context, TodoActivityRecorder activity, ILogger<TodoEmailActivityRecorder> logger)
    {
        this.context = context;
        this.activity = activity;
        this.logger = logger;
    }

    public async Task RecordSentAsync(
        IReadOnlyList<string> workflowTags, string subject, IReadOnlyList<string> to, string actorEmail, CancellationToken cancellationToken)
    {
        var numbers = TodoTagNumbers(workflowTags);
        if (numbers.Count == 0) return;
        try
        {
            var items = await context.TodoItems
                .Where(item => numbers.Contains(item.Number))
                .ToListAsync(cancellationToken);
            foreach (var item in items)
                activity.Record(item, TodoActivityKind.EmailSent, TodoActivitySummaries.EmailSentSummary(to, subject), actorEmail);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "The sent email \"{Subject}\" could not be written to the to-do timeline.", subject);
        }
    }

    // "JPMS/TODO-0083" → 83. Any tag that isn't a to-do tag is ignored.
    public static IReadOnlyList<int> TodoTagNumbers(IEnumerable<string> workflowTags)
    {
        const string todoStem = TriageCategories.WorkflowPrefix + "TODO-";
        var numbers = new List<int>();
        foreach (var tag in workflowTags)
        {
            if (!tag.StartsWith(todoStem, StringComparison.OrdinalIgnoreCase)) continue;
            if (int.TryParse(tag[todoStem.Length..], out var number)) numbers.Add(number);
        }
        return numbers;
    }
}
