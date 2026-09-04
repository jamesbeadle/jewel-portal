using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Api.Features.WeeklyCashflow;
using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Contracts.WeeklyCashflow.Export;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The Weekly Cashflow grid as the page and its Excel export read it (2026-09-04): Xero's
/// outstanding bills and invoices seeded into their weeks, the accountant's placements and
/// exclusions applied, folded into one line per supplier (a supplier group is one line), with a
/// column per week — the same WeeklyCashflowMaths and WeeklyCashflowExportBands the page uses,
/// so the connector's answer is the screen's answer. get_weekly_cashflow_plan stays the raw
/// overlay; this is the picture. The bank-anchored closing balance rides along only for the
/// directors, mirroring GetXeroCashSummaryEndpoint's gate.
/// </summary>
internal static partial class AiWeeklyCashflowGridTool
{
    public const string Name = "get_weekly_cashflow_grid";
    private const string ForceArgument = "force";
    private const string IncludeEntriesArgument = "includeEntries";

    // camelCase throughout, so a projected record member (entry.Label) reads like its neighbours.
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    // Mirror of GetXeroCashSummaryEndpoint.AllowedToViewCash — the bank position is directors only.
    private static readonly RoleSet AllowedToViewCash = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector);

    public static IReadOnlyList<AiTool> Build() => new List<AiTool>
    {
        new(
            Name,
            "The 13-week Weekly Cashflow grid as the accountant reads it on the page and in its Excel "
            + "export: cash in (client invoices) and cash out (supplier bills, then each manual band) "
            + "with a totals row per band and ONE LINE PER SUPPLIER, client or item — a supplier group "
            + "is one line — each line giving its amount per week (the current week carries everything "
            + "overdue; Later is beyond the horizon) and whether the accountant moved money into that "
            + "week. Net movement per week; for directors also the closing bank balance. Use this to "
            + "answer 'what do we pay whom, which week' — it is Xero-seeded and placement-adjusted, so "
            + "quote it over the raw plan. Pass includeEntries for the bills behind each line.",
            AiToolSchema.Object(
                (ForceArgument, "boolean", "true bypasses the server's short Xero cache for a fresh read.", false),
                (IncludeEntriesArgument, "boolean", "true lists every bill, invoice and occurrence under its line (large).", false)),
            AiToolKind.Read,
            WeeklyCashflowGates.WeeklyCashflowRoles,
            RunAsync)
    };

    private static async Task<string> RunAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var force = AiToolSchema.Flag(input, ForceArgument) ?? false;
        var payables = await Ask<GetXeroAgedPayables, XeroAgedPayablesSnapshot>(context, new GetXeroAgedPayables(force), ct);
        if (!payables.IsConfigured) return Fail("Xero is not configured on this server.");
        if (payables.Error is not null) return Fail($"Xero refused the bills read: {payables.Error}");
        var receivables = await Ask<GetXeroAgedReceivables, XeroAgedReceivablesSnapshot>(context, new GetXeroAgedReceivables(force), ct);
        if (receivables.Error is not null) return Fail($"Xero refused the invoices read: {receivables.Error}");
        var plan = await Ask<GetWeeklyCashflowPlan, WeeklyCashflowPlan>(context, new GetWeeklyCashflowPlan(), ct);

        var seeds = payables.Bills
            .Select(WeeklyCashflowSeeding.FromBill)
            .Concat(receivables.Invoices.Select(WeeklyCashflowSeeding.FromInvoice));
        var (counted, excluded) = WeeklyCashflowSeeding.Split(seeds, plan.Exclusions);
        var view = WeeklyCashflowMaths.Build(
            SiteClock.Today(), counted, plan.Items, plan.Placements, await OpeningBalanceFor(context, force, ct));
        var bands = WeeklyCashflowExportBands.For(view, plan.SupplierGroups);
        var includeEntries = AiToolSchema.Flag(input, IncludeEntriesArgument) ?? false;
        return Serialise(Shape(view, bands, excluded, plan.Exclusions, payables, includeEntries));
    }

    // The bank position only for those the cash-summary endpoint would serve; null leaves the
    // closing balance out of the grid exactly as the page does for Accounts.
    private static async Task<decimal?> OpeningBalanceFor(AiToolContext context, bool force, CancellationToken ct)
    {
        if (!AllowedToViewCash.IncludesAny(context.User.Roles)) return null;
        var cash = await Ask<GetXeroCashSummary, XeroCashSummarySnapshot>(context, new GetXeroCashSummary(force), ct);
        if (!cash.IsConfigured || cash.Error is not null) return null;
        return cash.TotalCash;
    }

    private static Task<TResult> Ask<TQuery, TResult>(AiToolContext context, TQuery query, CancellationToken ct)
        where TQuery : IQuery<TResult> =>
        context.Services
            .GetRequiredService<IQueryHandler<TQuery, TResult>>()
            .HandleAsync(query, ct);

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);

    private static string Fail(string message) => Serialise(new { ok = false, error = message });
}
