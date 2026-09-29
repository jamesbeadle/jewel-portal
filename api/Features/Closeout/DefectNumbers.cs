namespace Jewel.JPMS.Api.Features.Closeout;

/// <summary>The DEF-#### sequence: global (like to-do numbers), max + 1 and never a row count — a
/// deleted row must not re-issue its number, because the number is the mailbox tag stem
/// ("JPMS/DEF-0001"). One minter for every door a defect comes in by.</summary>
internal static class DefectNumbers
{
    public static async Task<int> NextAsync(JpmsContext context, CancellationToken cancellationToken) =>
        (await context.Defects.MaxAsync(row => (int?)row.Number, cancellationToken) ?? 0) + 1;
}
