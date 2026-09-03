using Jewel.JPMS.Contracts.Kpi;

namespace Jewel.JPMS.Api.Features.Kpi.Commands;

// Administrators only — see KpiRoles. One gate per command so the connector's action registry
// resolves an unambiguous Allows/Check per command type.
public sealed class MarkEmailAsKpiAuthorisation
{
    public bool Allows(SignedInUser user, MarkEmailAsKpi command) => KpiRoles.IsAdministrator(user);
}

public sealed class MarkEmailAsKpiValidation
{
    public ValidationOutcome Check(MarkEmailAsKpi command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.MessageId)) errors.Add("MessageId is required.");
        if (string.IsNullOrWhiteSpace(command.PersonId) && string.IsNullOrWhiteSpace(command.PersonEmail) && string.IsNullOrWhiteSpace(command.PersonName))
            errors.Add("Say who the KPI is about: personId, personEmail (a portal user) or personName (someone without a login).");
        if (command.Note is { Length: > 2048 }) errors.Add("Note is limited to 2048 characters.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class UpdateKpiEmailAuthorisation
{
    public bool Allows(SignedInUser user, UpdateKpiEmail command) => KpiRoles.IsAdministrator(user);
}

public sealed class UpdateKpiEmailValidation
{
    public ValidationOutcome Check(UpdateKpiEmail command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.KpiEmailId)) errors.Add("KpiEmailId is required.");
        if (string.IsNullOrWhiteSpace(command.PersonId) && string.IsNullOrWhiteSpace(command.PersonEmail) && string.IsNullOrWhiteSpace(command.PersonName))
            errors.Add("Say who the KPI is about: personId, personEmail (a portal user) or personName (someone without a login).");
        if (command.Note is { Length: > 2048 }) errors.Add("Note is limited to 2048 characters.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class RemoveKpiEmailAuthorisation
{
    public bool Allows(SignedInUser user, RemoveKpiEmail command) => KpiRoles.IsAdministrator(user);
}

public sealed class AddKpiPersonAuthorisation
{
    public bool Allows(SignedInUser user, AddKpiPerson command) => KpiRoles.IsAdministrator(user);
}

public sealed class AddKpiPersonValidation
{
    public ValidationOutcome Check(AddKpiPerson command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.Name) && string.IsNullOrWhiteSpace(command.Email))
            errors.Add("Name is required (or a portal user's email to link).");
        if (command.Name is { Length: > 256 }) errors.Add("Name is limited to 256 characters.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
