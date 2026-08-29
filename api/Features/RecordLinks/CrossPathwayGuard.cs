namespace Jewel.JPMS.Api.Features.RecordLinks;

/// <summary>
/// The cross-filing pre-flight, checked BEFORE a record is created from an email.
///
/// <para>RETIRED 2026-08-28 — cross-filing no longer needs a confirm. The pathway panes make the
/// second filing an explicit, visible choice (each pane names the pathway it files under, and the
/// pane hint says the thread is already filed elsewhere), so the "Confirm the cross-filing" reject
/// this guard used to throw was a second ask for a decision the triager had already made on
/// screen. Dual filing is simply allowed: the link path stamps the new bucket alongside the
/// existing one, exactly as the confirmed path always did.</para>
///
/// <para>The method and its call sites are kept (as a no-op) rather than deleted: they mark the
/// exact spot the pre-flight ran — BEFORE anything persists — which is where a check would have to
/// go again if a pathway rule ever returns. (The original guard existed because the
/// create-from-message commands persist the record first and link the email after; a rejection on
/// the link path left the record created, and a retry raised a duplicate — the 2026-08-22
/// "WO-CA63BC67" glitch.)</para>
/// </summary>
public static class CrossPathwayGuard
{
    /// <summary>
    /// Formerly threw the "Confirm the cross-filing" reject when filing <paramref name="bucket"/>
    /// would put the thread under a second pathway without <paramref name="allowCrossPathway"/>.
    /// Now a no-op — cross-filing is allowed without a confirm (retired 2026-08-28, see the class
    /// note). The parameters are kept so call sites (and the AllowCrossPathway contract param)
    /// stay source-compatible.
    /// </summary>
    public static void EnsureConfirmed(
        IEnumerable<string>? categories, string? bucket, bool allowCrossPathway, string newRecordLabel)
    {
        // Intentionally nothing.
    }
}
