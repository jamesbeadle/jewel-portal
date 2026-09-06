using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Sales;

/// <summary>
/// Moves a lead along (or back down) the ladder — any stage except Won, which is WinLead
/// because it creates records. Lost takes a reason; reopening a Lost or Nurture lead clears it.
/// Writes a StageChange activity on the lead's timeline, with the note if one is given.
/// ChangedByEmail is stamped by the server.
/// </summary>
public sealed record MoveLeadStage(
    string LeadId,
    LeadStage Stage,
    string? Note,
    string? LostReason,
    string ChangedByEmail = "") : ICommand<Lead>;
