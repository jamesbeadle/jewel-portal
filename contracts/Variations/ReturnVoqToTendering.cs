using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Returns an approved VOQ to Tendering, un-doing the approval — for records that were approved in
/// error (chiefly seeded history marked Approved when the client never actually approved it). The
/// live VO is deleted (freeing its V-ref for a future approval) and the commercial writes made at
/// approval are reversed; refused when the VO has been instructed (work orders) or claimed against.
/// </summary>
public sealed record ReturnVoqToTendering(string VariationOrderQuoteId) : ICommand<VariationOrderQuote>;
