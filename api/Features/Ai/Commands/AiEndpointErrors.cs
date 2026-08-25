namespace Jewel.JPMS.Api.Features.Ai.Commands;

/// <summary>
/// Turns an exception from a turn endpoint into something the person reading the chat panel can
/// act on. Recognises the failures that are configuration rather than bugs; everything else falls
/// back to the exception's own message, which is more use than nothing.
///
/// <para>Reads the INNERMOST exception, not the outer one. EF wraps the database's own words in
/// "An error occurred while saving the entity changes. See the inner exception for details." —
/// and the inner exception is the one that names the missing table (live, twice on 2026-08-25:
/// AiAttachments, then AiPendingReplies, each shipped ahead of its script being run). Shared by
/// every turn endpoint so they all say the same thing about the same failure.</para>
/// </summary>
internal static class AiEndpointErrors
{
    public static string Explain(Exception ex)
    {
        var root = ex;
        while (root.InnerException is not null) root = root.InnerException;
        var message = ReferenceEquals(root, ex) ? ex.Message : $"{ex.Message} {root.Message}";

        if (message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase))
        {
            return "The assistant's database tables are missing or out of date. "
                + "A pending EF migration has not been applied to this environment. "
                + $"({message})";
        }

        if (message.Contains("Unable to resolve service", StringComparison.OrdinalIgnoreCase))
        {
            return $"The assistant is not wired up correctly on this environment. ({message})";
        }

        return $"The assistant hit an unexpected error. ({message})";
    }
}
