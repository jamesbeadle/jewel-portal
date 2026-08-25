using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Commands;

public sealed class SendAiMessageValidation
{
    private const int MaxMessageLength = 8000;

    /// <summary>The live contents of the dialog beside the chat. Generous for any real form — a
    /// PQQ response (twenty-odd questions with prose answers, 2026-08-25) is the largest — and a
    /// ceiling on what an unbounded client field can push into the prompt every single turn.</summary>
    private const int MaxDraftLength = 60_000;

    public ValidationOutcome Check(SendAiMessage command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.Message)) errors.Add("Type a message first.");
        if (command.Message is { Length: > MaxMessageLength })
            errors.Add($"That message is too long ({MaxMessageLength} characters max).");

        if (command.Scope?.Task is { } task)
        {
            // An unknown key means a client naming a dialog that does not exist. Refused here rather
            // than ignored, so it cannot quietly become a conversation with no task rules in force.
            if (ModalCatalog.Find(task.ModalKey) is null)
                errors.Add("That dialog is not one the assistant can work in.");
            if (task.DraftJson is { Length: > MaxDraftLength })
                errors.Add("There is too much in that form to send with a message.");
        }

        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
