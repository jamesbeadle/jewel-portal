namespace Jewel.JPMS.Components.Chat;

/// <summary>
/// The starter prompts in the empty state. They change with the page, because a fixed set is
/// noise — three suggestions about variations on the mailbox triage screen teach the user that the
/// panel is not paying attention.
///
/// <para>Matched on the route rather than the page label so a renamed nav item does not silently
/// drop a page back to the generic set.</para>
/// </summary>
public static class ChatSuggestions
{
    public static IReadOnlyList<string> For(string path, string? projectReference)
    {
        var route = path.TrimEnd('/');
        var project = string.IsNullOrWhiteSpace(projectReference) ? "this project" : projectReference;

        if (route.Contains("/variations", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                $"Which variations on {project} are awaiting an AI?",
                "What is the total value of everything still in Quoting?",
                "Which one has been out longest?"
            };
        }

        if (route.Contains("/requests", StringComparison.OrdinalIgnoreCase)
            || route.Contains("/rfis", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                $"What is open on {project} and who are we waiting on?",
                "Which of these are on the critical path?",
                "Anything overdue a response?"
            };
        }

        // The Control Centre — matches its route and the legacy /requests/triage alias.
        if (route.Contains("control-centre", StringComparison.OrdinalIgnoreCase)
            || route.Contains("triage", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                "What is this email about and does it match an open request?",
                $"What is still open on {project}?",
                "Find RFI-049"
            };
        }

        if (route.Contains("/programme", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                $"What is the contract completion date on {project}?",
                "Which requests are flagged as critical path?",
                "Has a notice of delay been raised?"
            };
        }

        if (route.Contains("/valuation", StringComparison.OrdinalIgnoreCase)
            || route.Contains("/financials", StringComparison.OrdinalIgnoreCase)
            || route.Contains("/finance", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                $"What is the retention position on {project}?",
                "What are the payment notice periods under the contract?",
                "Which approved variations are not yet claimed?"
            };
        }

        if (route.Contains("/contract", StringComparison.OrdinalIgnoreCase)
            || route.Contains("/settings", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                $"Summarise the contract terms on {project}",
                "What OH&P applies to a variation here?",
                "When is the application cut-off each month?"
            };
        }

        if (route.Contains("/projects", StringComparison.OrdinalIgnoreCase) && route.Length > "/projects".Length)
        {
            return new[]
            {
                $"Where does {project} stand this week?",
                "What is open and who are we waiting on?",
                "What does the contract say about retention?"
            };
        }

        // Home, the portfolio, and anything unmatched.
        return new[]
        {
            "Which projects are live and what stage is each at?",
            "Find V72",
            "What is awaiting an architect's instruction anywhere?"
        };
    }
}
