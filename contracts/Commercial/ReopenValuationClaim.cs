using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

// Undoes an unintended preapproval: Preapproved -> Draft. The frozen totals are cleared
// (a Draft computes live from its entries) and the preapproval stamp removed, so the
// claim is editable again as if "We're claiming this" had never been clicked. Confirmed
// claims are final and cannot be reopened.
public sealed record ReopenValuationClaim(string ValuationClaimId) : ICommand<ValuationClaim>;
