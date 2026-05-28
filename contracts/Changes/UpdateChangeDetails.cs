using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Changes;

public sealed record UpdateChangeDetails(
    string ChangeRecordId,
    string Reference,
    string Title,
    string Description,
    ChangeStatus Status,
    decimal? Value,
    string? ResponseText,
    string? RespondedByEmail,
    bool ImpliesVariation) : ICommand<ChangeRecord>;
