using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class SaveWeeklyCashflowSupplierGroupValidation
{
    // The stored member list is one nvarchar(4000) JSON array; the caps keep any sensible group
    // comfortably inside it (40 × ~90 chars of name + JSON packaging).
    private const int MaxMembers = 40;
    private const int MaxStoredJsonLength = 4000;

    public ValidationOutcome Check(SaveWeeklyCashflowSupplierGroup command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.Name))
            errors.Add("A group name is required.");
        if (command.Name?.Length > 200)
            errors.Add("The group name is too long (200 characters at most).");

        var members = (command.ContactNames ?? Array.Empty<string>())
            .Select(name => name?.Trim() ?? "")
            .Where(name => name.Length > 0)
            .ToList();

        if (members.Count == 0)
            errors.Add("A group needs at least one supplier.");
        if (members.Count > MaxMembers)
            errors.Add($"A group holds {MaxMembers} suppliers at most.");
        if (members.Any(name => name.Length > 200))
            errors.Add("A supplier name is too long (200 characters at most).");
        if (WeeklyCashflowEntityMapping.WriteContactNames(members).Length > MaxStoredJsonLength)
            errors.Add("The group's supplier list is too long to store — split it into two groups.");

        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
