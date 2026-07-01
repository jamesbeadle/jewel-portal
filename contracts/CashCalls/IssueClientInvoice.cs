using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.CashCalls;

/// <summary>Marks a cash call's client invoice as prepared (Requested -> Invoiced).</summary>
public sealed record IssueClientInvoice(string CashCallId) : ICommand<CashCall>;
