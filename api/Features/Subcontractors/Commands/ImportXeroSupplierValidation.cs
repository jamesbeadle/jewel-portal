using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class ImportXeroSupplierValidation
{
    public ValidationOutcome Check(ImportXeroSupplier command)
    {
        if (string.IsNullOrWhiteSpace(command.XeroContactId))
            return ValidationOutcome.Failed("A Xero contact id is required.");
        return ValidationOutcome.Passed;
    }
}
