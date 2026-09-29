
namespace Jewel.JPMS.Api.Features.Manual.Commands;

/// <summary>Every value a command writes, checked against its column before any handler runs.</summary>
public sealed class ManualValidationGate
{
    public ValidationOutcome Check(CreateManualModule command)
    {
        var errors = new List<string>();
        if (!ManualModuleCodes.IsWellFormed(command.Code.Trim().ToUpperInvariant()))
            errors.Add("Code must be a family and a number, like RUN-01.");
        ManualFieldChecks.Text(errors, "Title", command.Title, ManualLimits.Title, isRequired: true);
        ManualFieldChecks.Text(errors, "Purpose", command.Purpose, ManualLimits.Purpose, isRequired: false);
        ManualFieldChecks.Text(errors, "SourceSections", command.SourceSections, ManualLimits.SourceSections, isRequired: false);
        ManualFieldChecks.Controls(errors, command.OwnerEmail, command.ApproverEmail, command.LinkedFormSlugs, command.LinkedStandards);
        return ManualFieldChecks.Outcome(errors);
    }

    public ValidationOutcome Check(UpdateManualModuleDraft command)
    {
        var errors = new List<string>();
        ManualFieldChecks.Identifier(errors, command.ManualModuleId);
        ManualFieldChecks.Text(errors, "Title", command.Title, ManualLimits.Title, isRequired: true);
        ManualFieldChecks.Text(errors, "Purpose", command.Purpose, ManualLimits.Purpose, isRequired: false);
        ManualFieldChecks.Text(errors, "ChangeSummary", command.ChangeSummary, ManualLimits.ChangeSummary, isRequired: false);
        ManualFieldChecks.Controls(errors, command.OwnerEmail, command.ApproverEmail, command.LinkedFormSlugs, command.LinkedStandards);
        return ManualFieldChecks.Outcome(errors);
    }

    public ValidationOutcome Check(SubmitManualModuleForReview command) => ManualFieldChecks.IdentifierOnly(command.ManualModuleId);

    public ValidationOutcome Check(ApproveManualModule command) => ManualFieldChecks.IdentifierOnly(command.ManualModuleId);

    public ValidationOutcome Check(ReturnManualModuleToDraft command)
    {
        var errors = new List<string>();
        ManualFieldChecks.Identifier(errors, command.ManualModuleId);
        ManualFieldChecks.Text(errors, "Reason", command.Reason, ManualLimits.ChangeSummary, isRequired: true);
        return ManualFieldChecks.Outcome(errors);
    }

    public ValidationOutcome Check(ReviseManualModule command) => ManualFieldChecks.IdentifierOnly(command.ManualModuleId);

    public ValidationOutcome Check(RetireManualModule command) => ManualFieldChecks.IdentifierOnly(command.ManualModuleId);

    public ValidationOutcome Check(AcknowledgeManualModule command)
    {
        var errors = new List<string>();
        ManualFieldChecks.Identifier(errors, command.ManualModuleId);
        ManualFieldChecks.Text(errors, "TypedName", command.TypedName, ManualLimits.TypedName, isRequired: true);
        return ManualFieldChecks.Outcome(errors);
    }

    public ValidationOutcome Check(ImportManualBaseline command) => ValidationOutcome.Passed;
}
