using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

public sealed record ReviseQuote(
    string QuoteId,
    decimal Value,
    string Notes) : ICommand<Quote>;
