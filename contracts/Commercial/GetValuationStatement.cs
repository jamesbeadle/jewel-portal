using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// A valuation as its statement: the claim with its lines in statement order — the frozen rows
/// of a locked valuation, or the working copy computed from the live bill for a Draft. Feeds the
/// read-only statement viewer, the PDF, the spreadsheet and the connector.
/// <see cref="ValuationClaimId"/> accepts a retired valuation-report-snapshot id too: until
/// 2026-09-18 the statement was a separate object, and links, invoices and mailbox tags that
/// still name one resolve to the valuation it was frozen from (ValuationClaimLegacyStatements).
/// </summary>
public sealed record GetValuationStatement(string ValuationClaimId) : IQuery<ValuationStatement>;
