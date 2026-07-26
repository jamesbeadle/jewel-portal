using Jewel.JPMS.Api.Gates;

namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// Who is talking to the assistant on this invocation. Scoped, set by the endpoint immediately after
/// the auth gate — the same pattern as <c>Audit.AuditActor</c>, and for the same reason: commands
/// deliberately do not carry the caller's identity, but the tool layer needs the caller's *roles*,
/// not just their email, to filter the catalogue.
///
/// <para>Never populate this from anything the client sends.</para>
/// </summary>
public sealed class AiCaller
{
    public SignedInUser? Current { get; set; }
}
