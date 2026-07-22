namespace Jewel.JPMS.Api.Features.Audit;

/// <summary>Who is performing the current request, for audit rows. Scoped per invocation and set by
/// the endpoint gates right after they resolve the signed-in user — commands deliberately don't
/// carry the caller's identity, so this is how it reaches the handlers' audit writes. Empty when
/// nothing set it (e.g. the worker), in which case writers fall back to their own label.</summary>
public sealed class AuditActor
{
    public string Email { get; set; } = "";
}
