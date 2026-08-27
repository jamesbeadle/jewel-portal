using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.BuildingControl;

/// <summary>
/// The Control Centre's "Raise Building Control Inspection": turns the inspector's email — a
/// booking confirmation, a visit arrangement — into an inspection stage on the project's case
/// and tags the email to it (JPMS/BCI-####), so the inspection reads its thread back live.
/// Requires the project's building control case to exist already (set up on the Building Control
/// tab) — an inspection can never be raised into thin air. CreatedByEmail is stamped server-side
/// from the signed-in user.
/// </summary>
public sealed record CreateBuildingControlInspectionFromMessage(
    string MessageId,
    string? InternetMessageId,
    string ProjectId,
    BuildingControlInspectionDetails Details,
    string CreatedByEmail = "",
    LinkThreadScope Scope = LinkThreadScope.ThreadBehindAnchor,
    // Explicit consent to file the thread under an additional pathway it already carries.
    // Pre-flighted before anything is created (CrossPathwayGuard), so a rejection creates nothing.
    bool AllowCrossPathway = false) : ICommand<BuildingControlInspection>;
