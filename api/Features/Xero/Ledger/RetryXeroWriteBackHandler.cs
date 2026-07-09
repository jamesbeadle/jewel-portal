using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// Explicit re-attempt of the Xero write-back for one invoice — the allocation
/// page's Retry button after a failed confirmation (unmapped site, Xero outage,
/// tracking-option rejection...). The service re-checks eligibility from scratch,
/// so a retry can also succeed "silently" when the invoice was approved in Xero
/// in the meantime.
/// </summary>
public sealed class RetryXeroWriteBackHandler : ICommandHandler<RetryXeroWriteBack, XeroWriteBackOutcome>
{
    private readonly IXeroWriteBackService writeBack;

    public RetryXeroWriteBackHandler(IXeroWriteBackService writeBack) { this.writeBack = writeBack; }

    public Task<XeroWriteBackOutcome> HandleAsync(RetryXeroWriteBack command, CancellationToken cancellationToken) =>
        writeBack.RetryAsync(command.XeroInvoiceId, cancellationToken);
}
