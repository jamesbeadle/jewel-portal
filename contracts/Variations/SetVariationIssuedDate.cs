using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Corrects the date a variation was issued to the client (2026-09-24, Jeremy, from By France
/// Report 31: V81, V87 and V89 were moved to Issued in the portal days after PLG had them, so the
/// Contractor's Report's Position read the portal's date, not the client's). The Issued stamp is
/// the official date the client was notified, so a person may set it to the day that happened.
/// Only a variation that has been issued carries one; a date in the future is refused.
/// </summary>
public sealed record SetVariationIssuedDate(string VariationOrderId, DateOnly IssuedOn) : ICommand<VariationOrder>;
