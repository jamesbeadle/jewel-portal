using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Rates;

public sealed record ReviseRate(
    string RateId,
    string Trade,
    string Description,
    string Unit,
    decimal Value,
    string SupplierName) : ICommand<Rate>;
