using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Drawings;

public sealed record IssueDrawingRevision(
    string DrawingId,
    string RevisionLabel,
    string FileName,
    string IssuedByEmail) : ICommand<DrawingRevision>;
