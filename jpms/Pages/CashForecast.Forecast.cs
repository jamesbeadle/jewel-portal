using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Pages;

public partial class CashForecast
{
    // ---- The forecast --------------------------------------------------------------------
    // The engine (CashForecastPhasing, contracts) owns every phasing rule; this page only
    // prepares its inputs from the statement figures above and renders what comes back.

    private sealed record ForecastRowInfo(ForecastCategory Category, string Label, string Source);

    private static readonly ForecastRowInfo[] InRows =
    {
        new(ForecastCategory.InvoicesOutstanding, "Valuation invoices outstanding",
            "already issued (or awaiting approval) · lands a payment-mechanism lag after issue"),
        new(ForecastCategory.FutureValuations, "Future valuations",
            "left to claim — spread evenly to practical completion, or claimed at the project's expected £/month where set — each valuation paid its contract's payment terms after the valuation date"),
        new(ForecastCategory.RetentionReleases, "Retention releases",
            "R1 at practical completion · R2 after the defects period")
    };

    private static readonly ForecastRowInfo[] OutRows =
    {
        new(ForecastCategory.BillsUnpaid, "Supplier bills unpaid",
            "part-payment aware · assumed payable this month (per-bill due dates pending)"),
        new(ForecastCategory.WorkOrdersToInvoice, "Work orders still to invoice",
            "committed less invoiced, spread to practical completion, paid a month later"),
        new(ForecastCategory.DrawdownsToSpend, "Drawdowns still to spend",
            "budget beyond orders and bills, spread to practical completion, paid a month later")
    };

    private DateTimeOffset Now => DateTimeOffset.Now;

    private ProjectForecastInputs ForecastInputsFor(Project project)
    {
        var projectId = project.ProjectId;
        var row = RowFor(projectId);
        var figures = FiguresFor(projectId);
        var schedule = ScheduleFor(projectId, figures);
        var contract = Contracts.ForProject(projectId);
        var terms = retentionByProject.TryGetValue(projectId, out var retention) ? retention : null;

        // §4.1 — the contract's payment mechanism: notice days then final date for payment.
        var lagDays = contract is { } paymentTerms
            ? Math.Max(1, paymentTerms.PaymentNoticeDays + paymentTerms.FinalDateForPaymentDays)
            : DefaultPaymentLagDays;

        // Itemised outstanding invoices at their expected receipt dates. Awaiting-approval
        // invoices count one month later than issued ones — the approval loop's cost.
        var receipts = new List<DatedAmount>();
        decimal issuedOutstanding = 0m, awaitingApproval = 0m;
        foreach (var invoice in invoicesByProject.TryGetValue(projectId, out var list)
                     ? list : (IReadOnlyList<ValuationInvoice>)Array.Empty<ValuationInvoice>())
        {
            if (invoice.Status == ValuationInvoiceStatus.Issued)
            {
                var outstanding = invoice.Amount - invoice.AmountPaid;
                if (outstanding == 0m) continue;
                issuedOutstanding += outstanding;
                receipts.Add(new DatedAmount(outstanding, invoice.IssuedAt?.AddDays(lagDays)));
            }
            else if (invoice.IsAwaitingApproval)
            {
                awaitingApproval += invoice.Amount;
                receipts.Add(new DatedAmount(invoice.Amount, Now.AddMonths(1).AddDays(lagDays)));
            }
        }

        // §4.2 — the remainder still to be valued: left to claim less the two invoice buckets,
        // NOT floored — subtracting keeps the in-total exactly LeftToClaim, which is what makes
        // the tie-back to the statement exact.
        var futureValuations = row.LeftToClaim - issuedOutstanding - awaitingApproval;

        // The spread anchor: practical completion from the retention terms, else the contract's
        // completion date, else nothing honest — the engine sends the spreads to Undated.
        var practicalCompletion = terms?.PracticalCompletionAt ?? contract?.CompletionDate;

        return new ProjectForecastInputs(
            projectId,
            receipts,
            futureValuations,
            project.NextExpectedValuationDate,
            practicalCompletion,
            lagDays,
            new DatedAmount(row.Release1, schedule?.CompletionRelease.DueOn),
            new DatedAmount(row.Release2, schedule?.FinalRelease.DueOn),
            row.BillsUnpaid,
            row.WoLeftToInvoice,
            row.Drawdown,
            // The FD's per-project view of the certifying rate — null keeps the even spread.
            project.ExpectedMonthlyValuation);
    }

    private sealed record ForecastView(
        DateTime[] Axis,
        IReadOnlyDictionary<ForecastCategory, decimal[]> Cells,
        IReadOnlyDictionary<ForecastCategory, decimal> Later,
        IReadOnlyDictionary<ForecastCategory, decimal> Undated,
        IReadOnlyDictionary<ForecastCategory, List<(Project Project, PhasedCategory Phased)>> PerProject,
        decimal[] ProjectNet,
        decimal[] Net,
        decimal LaterNet,
        decimal[] Closing,
        int MinIndex,
        List<(Project Project, decimal Variance)> Variances);

    // Built ONCE per render, at the top of the markup, and threaded through — the same cost
    // profile the retired page accepted for Totals().
    private ForecastView BuildForecast()
    {
        var asOf = Now;
        var start = CashForecastPhasing.MonthOf(asOf);
        var projects = SelectedProjects;
        var inputs = projects.Select(project => (Project: project, Inputs: ForecastInputsFor(project))).ToList();

        var horizonEnd = CashForecastPhasing.HorizonEndFor(inputs.Select(pair => pair.Inputs), asOf);
        var monthCount = Math.Clamp(CashForecastPhasing.MonthsBetween(start, horizonEnd) + 1, 1, MaxVisibleMonths);
        var axis = Enumerable.Range(0, monthCount).Select(offset => start.AddMonths(offset)).ToArray();

        var categories = Enum.GetValues<ForecastCategory>();
        var cells = categories.ToDictionary(category => category, _ => new decimal[monthCount]);
        var later = categories.ToDictionary(category => category, _ => 0m);
        var undated = categories.ToDictionary(category => category, _ => 0m);
        var perProject = categories.ToDictionary(
            category => category, _ => new List<(Project Project, PhasedCategory Phased)>());
        var variances = new List<(Project Project, decimal Variance)>();

        foreach (var (project, projectInputs) in inputs)
        {
            var phased = CashForecastPhasing.Phase(projectInputs, asOf, monthCount);
            foreach (var category in categories)
            {
                var answer = phased.Categories[category];
                for (var index = 0; index < monthCount; index++) cells[category][index] += answer.Months[index];
                later[category] += answer.Later;
                undated[category] += answer.Undated;
                if (answer.Total != 0m) perProject[category].Add((project, answer));
            }

            // The promise: phased buckets must reproduce the statement's completion cashflow.
            var variance = phased.CompletionCashflow - RowFor(project.ProjectId).ProjectCompletionCashflow;
            if (variance != 0m) variances.Add((project, variance));
        }

        // Project movement first — the ticked projects' own cash in less cash out (FD,
        // 2026-08-17) — then net movement, which is that less the month's company overheads.
        var projectNet = new decimal[monthCount];
        var net = new decimal[monthCount];
        for (var index = 0; index < monthCount; index++)
        {
            var cashIn = InRows.Sum(row => cells[row.Category][index]);
            var cashOut = OutRows.Sum(row => cells[row.Category][index]);
            projectNet[index] = cashIn - cashOut;
            net[index] = projectNet[index] - OverheadsFor(axis[index]);
        }
        var laterNet = InRows.Sum(row => later[row.Category]) - OutRows.Sum(row => later[row.Category]);

        // The running balance, directors only — seeded from the bank position.
        var closing = Array.Empty<decimal>();
        var minIndex = 0;
        if (BankReady && BankSnapshot is { } bank)
        {
            closing = new decimal[monthCount];
            var balance = bank.TotalCash;
            for (var index = 0; index < monthCount; index++)
            {
                balance += net[index];
                closing[index] = balance;
                if (balance < closing[minIndex]) minIndex = index;
            }
        }

        return new ForecastView(axis, cells, later, undated, perProject, projectNet, net, laterNet, closing, minIndex, variances);
    }



    private void ToggleCategory(ForecastCategory category)
    {
        if (!expandedCategories.Remove(category)) expandedCategories.Add(category);
    }

    private async Task OnOverheadsChanged(ChangeEventArgs args)
    {
        overheadsMonthly = decimal.TryParse(args.Value?.ToString(), out var value) && value >= 0m ? value : 0m;
        if (Auth.CurrentUser is { } user) await OverheadsStorage.WriteAsync(user.Email, overheadsMonthly);
    }

    // One month's own figure. A cleared cell — or one set back to the default — drops the
    // override, so the month follows the default again (including future default changes).
    private async Task OnOverheadMonthChangedAsync(DateTime month, ChangeEventArgs args)
    {
        var isAmount = decimal.TryParse(args.Value?.ToString(), out var value) && value >= 0m;
        if (isAmount && value != overheadsMonthly) overheadsOverrides[month] = value;
        else overheadsOverrides.Remove(month);
        if (Auth.CurrentUser is { } user) await OverheadsStorage.WriteOverridesAsync(user.Email, overheadsOverrides);
    }

}
