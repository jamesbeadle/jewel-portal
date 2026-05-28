using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Mobilisation;

public sealed record UpdateMobilisationChecklistItem(
    string MobilisationItemId,
    string Description,
    string OwnerEmail,
    bool IsComplete) : ICommand<MobilisationItem>;
