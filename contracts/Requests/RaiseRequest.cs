using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Changes;

public sealed record RaiseChange(
    string ProjectId,
    ChangeKind Kind,
    string Reference,
    string Title,
    string Description,
    decimal? Value,
    string RaisedByEmail) : ICommand<ChangeRecord>;
