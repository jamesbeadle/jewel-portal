namespace Jewel.JPMS.Api.Features.SiteInstructions;

/// <summary>The SI-#### sequence: global (like defect and inventory numbers), max + 1 and never a
/// row count — a deleted row must not re-issue its number, because the number is the mailbox tag
/// stem ("JPMS/SI-0001"). One minter for every door an instruction comes in by.</summary>
internal static class SiteInstructionNumbers
{
    public static async Task<int> NextAsync(JpmsContext context, CancellationToken cancellationToken) =>
        (await context.SiteInstructions.MaxAsync(row => (int?)row.Number, cancellationToken) ?? 0) + 1;
}
