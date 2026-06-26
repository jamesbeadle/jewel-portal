using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

// "We are claiming this." Locks the Draft claim's amounts and moves it to Preapproved.
public sealed record PreapproveValuationClaim(string ValuationClaimId) : ICommand<ValuationClaim>;
