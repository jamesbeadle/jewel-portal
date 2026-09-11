using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.XeroPayments;

/// <summary>The same people who may record a payment by hand (RecordValuationInvoicePaymentAuthorisation).</summary>
public sealed class SyncValuationInvoicePaymentsAuthorisation
{
    public bool Allows(SignedInUser user, SyncValuationInvoicePaymentsFromXero command) =>
        ValuationInvoiceRoles.AllowedToManageValuationInvoices.IncludesAny(user.Roles);
}

public sealed class SyncValuationInvoicePaymentsValidation
{
    public ValidationOutcome Check(SyncValuationInvoicePaymentsFromXero command)
    {
        if (string.IsNullOrWhiteSpace(command.ProjectId)) return new ValidationOutcome(new[] { "ProjectId is required." });
        return ValidationOutcome.Passed;
    }
}
