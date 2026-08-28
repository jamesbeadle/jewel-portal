using Jewel.JPMS.Api.Gates;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// One portal action the connector can perform on the caller's behalf — a data-only mirror of one
/// command endpoint. The gateway (list_actions / describe_action / perform_action) turns these
/// into behaviour; an entry declares WHAT exists, never HOW to run it, so an entry is one
/// declaration long and the 200-odd of them stay reviewable.
///
/// <para>Every action executes through the SAME pieces its HTTP endpoint composes — the command's
/// Authorisation.Allows, Validation.Check, then its ICommandHandler — with the actor stamped
/// server-side from the authenticated connector user (<see cref="EmailStamps"/> /
/// <see cref="NameStamps"/> name the command properties the endpoint would stamp; they are
/// excluded from the model-facing schema and can never be supplied by the caller). A write from
/// an AI tool is indistinguishable in the record from a click — same gates, same handler, same
/// audit the handler writes.</para>
/// </summary>
/// <param name="Name">Tool-style snake_case name, unique across the registry ("approve_variation_order").</param>
/// <param name="Area">The feature area as shown in list_actions ("Variations", "Procurement"…).</param>
/// <param name="Description">What it does and what it changes — written for the model, side
/// effects first ("SENDS EMAIL to…" where true).</param>
/// <param name="CommandType">The contract command record.</param>
/// <param name="ResultType">The command's result type (ICommand&lt;TResult&gt;).</param>
/// <param name="AuthorisationType">The endpoint's authorisation class, resolved from DI.</param>
/// <param name="ValidationType">The endpoint's validation class, resolved from DI — null for the
/// commands whose endpoints have none (the handler's own guards are then the only checks, exactly
/// as over HTTP).</param>
/// <param name="VisibleTo">Mirror of the authorisation's role set — what list_actions filters by
/// (ADR-002: an action the caller could not perform is never described). The authorisation class
/// remains the enforcement at execution.</param>
/// <param name="EmailStamps">Command constructor parameters stamped with the caller's email.</param>
/// <param name="NameStamps">Command constructor parameters stamped with the caller's display name.</param>
/// <param name="Notes">Extra guidance surfaced by describe_action — prerequisites, id-resolution
/// hints, irreversibility warnings.</param>
public sealed record AiAction(
    string Name,
    string Area,
    string Description,
    Type CommandType,
    Type ResultType,
    Type AuthorisationType,
    Type? ValidationType,
    RoleSet VisibleTo,
    IReadOnlyList<string> EmailStamps,
    IReadOnlyList<string> NameStamps,
    string? Notes = null);

/// <summary>Implemented once per feature area (RequestsActions, VariationsActions…). The registry
/// discovers implementations by reflection at boot, so adding an area file is the whole job.</summary>
public interface IAiActionSource
{
    IEnumerable<AiAction> Build();
}
