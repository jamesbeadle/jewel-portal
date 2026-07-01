using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.CashCalls;

/// <summary>
/// Records the client's payment against a cash call (-> Received) and increases the project-level
/// cash-call total by the amount received.
/// </summary>
public sealed record RecordCashCallReceipt(
    string CashCallId,
    decimal AmountReceived) : ICommand<CashCall>;
