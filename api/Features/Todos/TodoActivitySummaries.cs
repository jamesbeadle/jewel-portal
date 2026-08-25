using System.Text;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Todos;

// The sentences the timeline shows, and the diff that decides which of them a full-row update
// earns. Pure — no context, no clock — so the rules are testable on their own. A full-row
// UpdateTodoItem can change several facts at once; each changed fact is its own line, in the
// order a reader would tell the story (state first, then who, then when, then wording).
public static class TodoActivitySummaries
{
    public sealed record Line(TodoActivityKind Kind, string Summary);

    public static IReadOnlyList<Line> ForUpdate(TodoItemEntity before, TodoItemEntity after)
    {
        var lines = new List<Line>();
        if (!before.IsComplete && after.IsComplete) lines.Add(new(TodoActivityKind.Completed, "Marked done"));
        if (before.IsComplete && !after.IsComplete) lines.Add(new(TodoActivityKind.Reopened, "Reopened"));
        if (before.AssigneeRole != after.AssigneeRole || !SameEmail(before.AssigneePersonEmail, after.AssigneePersonEmail))
            lines.Add(new(TodoActivityKind.Reassigned, $"Reassigned to {AssigneeLabel(after.AssigneeRole, after.AssigneePersonEmail)}"));
        if (before.DueAt != after.DueAt) lines.Add(new(TodoActivityKind.DueChanged, DueSummary(after.DueAt)));
        if (before.Title != after.Title) lines.Add(new(TodoActivityKind.Edited, $"Title changed to \"{after.Title}\""));
        if (before.Notes != after.Notes) lines.Add(new(TodoActivityKind.Edited, "Detail updated"));
        return lines;
    }

    public static string CreatedSummary(TodoItemEntity item) =>
        item.AssigneeRole is null ? "Added, unassigned" : $"Added for {AssigneeLabel(item.AssigneeRole, item.AssigneePersonEmail)}";

    public static string CreatedFromEmailSummary(TodoItemEntity item, string subject) =>
        $"{CreatedSummary(item)} from the email \"{subject}\"";

    public static string MovedSummary(string projectLabel) => $"Moved to {projectLabel}";

    public static string EmailSentSummary(IReadOnlyList<string> to, string subject) =>
        $"Emailed {string.Join(", ", to)} — \"{subject}\"";

    public static string ChaseSummary(string? note) =>
        string.IsNullOrWhiteSpace(note) ? "Chased" : $"Chased — {note.Trim()}";

    public static string AssigneeLabel(int? role, string? personEmail)
    {
        if (role is null) return "unassigned";
        var roleName = Humanise(((Role)role).ToString());
        return string.IsNullOrWhiteSpace(personEmail) ? roleName : $"{roleName} ({personEmail})";
    }

    private static string DueSummary(DateTimeOffset? dueAt) =>
        dueAt is null ? "Due date cleared" : $"Due date set to {dueAt.Value:d MMM yyyy}";

    private static bool SameEmail(string? left, string? right) =>
        string.Equals(left ?? "", right ?? "", StringComparison.OrdinalIgnoreCase);

    // "FinanceDirector" → "Finance Director". The enum name is the one label the api has; the
    // UI's RolePresentation may word a role differently (it is jpms-only).
    private static string Humanise(string enumName)
    {
        var words = new StringBuilder();
        foreach (var character in enumName)
        {
            if (char.IsUpper(character) && words.Length > 0) words.Append(' ');
            words.Append(character);
        }
        return words.ToString();
    }
}
