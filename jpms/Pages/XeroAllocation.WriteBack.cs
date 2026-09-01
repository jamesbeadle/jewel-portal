using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Pages;

public partial class XeroAllocation
{

    private async Task RetryWriteBackAsync(XeroLedgerLine line)
    {
        isApplying = true; errorMessage = null;
        try
        {
            var outcome = await Ledger.RetryWriteBackAsync(line.XeroInvoiceId);
            syncMessage = outcome.Succeeded
                ? $"Invoice {line.InvoiceNumber ?? line.XeroInvoiceId} confirmed and approved in Xero."
                : null;
            errorMessage = outcome.Succeeded ? null : outcome.Error;
        }
        catch (CommandFailedException failure) { errorMessage = failure.Message; }
        finally { isApplying = false; }
    }

}
