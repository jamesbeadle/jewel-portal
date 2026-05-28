using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Closeout;

public sealed record AgreeVatAnalysis(
    string ProjectId,
    decimal ZeroRatedAmount,
    decimal StandardRatedAmount,
    string Notes,
    bool IsClientConfirmed,
    bool IsArchitectConfirmed) : ICommand<VatAnalysis>;
