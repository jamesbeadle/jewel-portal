using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Sales;

/// <summary>
/// The lead has chosen Jewel: creates the Client account (the lead's company, else the contact's
/// name, with the contact as primary contact) and the project shell (reference and name as
/// given, the lead's owner as project manager unless another is named), links both to the lead
/// and moves it to Won. Runs once — a lead already Won is refused. DecidedByEmail is stamped
/// by the server.
/// </summary>
public sealed record WinLead(
    string LeadId,
    string ProjectReference,
    string ProjectName,
    string? ProjectManagerEmail,
    string? Note,
    string DecidedByEmail = "") : ICommand<LeadWonOutcome>;
