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
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Subcontractors;

namespace Jewel.JPMS.Pages;

public partial class ProjectWorkOrders
{

    // One workbook, two sheets: a roll-up matching the on-screen grouping (and the
    // tfoot total) and the flattened per-order lines the expanded groups show — both
    // respecting the current supplier search, same as the on-screen table. "Ignore
    // search" (offered while a search narrows the table) exports every line instead.
    private ExcelWorkbook? BuildExportWorkbook(bool ignoreSearch)
    {
        if (LiveOrders.Count == 0) return null;

        var exportLines = ignoreSearch ? AllLines : VisibleLines;
        var exportOrderCount = ignoreSearch ? LiveOrders.Count : VisibleOrderCount;
        var exportCommitted = exportLines.Sum(entry => entry.Line.LineTotal);
        var exportPaid = exportLines.Sum(entry => entry.Line.PaidToDate);
        var exportRemaining = KnownRemainingOf(exportLines);
        var exportInvoiced = exportLines.Sum(entry => InvoicedShare(entry));
        var filteredTotal = !ignoreSearch && Searching;

        var rows = RowsFrom(exportLines);
        var workbook = new ExcelWorkbook();

        if (groupBySupplier)
        {
            var suppliersSheet = workbook.AddSheet("Suppliers",
                new ExcelColumn("Supplier"),
                new ExcelColumn("Orders", ExcelFormat.Integer),
                new ExcelColumn("Committed", ExcelFormat.Currency),
                new ExcelColumn("Paid", ExcelFormat.Currency),
                new ExcelColumn("Remaining", ExcelFormat.Currency),
                new ExcelColumn("Left to invoice", ExcelFormat.Currency));
            foreach (var row in rows)
            {
                // Same honesty as the screen: an unknown payment position exports as a
                // blank cell, not a £0.00 paid / full-value remaining that reads as fact.
                suppliersSheet.AddRow(
                    row.Name,
                    row.OrderCount,
                    row.Committed,
                    row.PaidKnown ? (object)row.Paid : null,
                    row.PaidKnown ? (object)(row.KnownCommitted - row.Paid) : null,
                    AtPennies(row.Committed - row.Invoiced));
            }
            suppliersSheet.AddRow(
                filteredTotal ? "Total (filtered)" : "Total",
                exportOrderCount,
                exportCommitted,
                exportPaid,
                exportRemaining,
                AtPennies(exportCommitted - exportInvoiced));
        }
        else
        {
            var centresSheet = workbook.AddSheet("Cost centres",
                new ExcelColumn("Code"),
                new ExcelColumn("Cost centre"),
                new ExcelColumn("Orders", ExcelFormat.Integer),
                new ExcelColumn("Committed", ExcelFormat.Currency),
                new ExcelColumn("Paid", ExcelFormat.Currency),
                new ExcelColumn("Remaining", ExcelFormat.Currency),
                new ExcelColumn("Left to invoice", ExcelFormat.Currency));
            foreach (var row in rows)
            {
                // Same honesty as the screen: an unknown payment position exports as a
                // blank cell, not a £0.00 paid / full-value remaining that reads as fact.
                centresSheet.AddRow(
                    row.Code,
                    row.InMaster ? row.Name : null,
                    row.OrderCount,
                    row.Committed,
                    row.PaidKnown ? (object)row.Paid : null,
                    row.PaidKnown ? (object)(row.KnownCommitted - row.Paid) : null,
                    AtPennies(row.Committed - row.Invoiced));
            }
            centresSheet.AddRow(
                filteredTotal ? "Total (filtered)" : "Total",
                null,
                exportOrderCount,
                exportCommitted,
                exportPaid,
                exportRemaining,
                AtPennies(exportCommitted - exportInvoiced));
        }

        var ordersSheet = workbook.AddSheet("Work orders",
            new ExcelColumn("Code"),
            new ExcelColumn("Cost centre"),
            new ExcelColumn("Order"),
            new ExcelColumn("Supplier"),
            new ExcelColumn("Line"),
            new ExcelColumn("Status"),
            new ExcelColumn("Total", ExcelFormat.Currency),
            new ExcelColumn("Paid", ExcelFormat.Currency));
        foreach (var row in rows)
        {
            foreach (var line in LinesFrom(exportLines, row.Key))
            {
                var code = CodeOf(line);
                var centreName = CostCentreNameFor(code);
                ordersSheet.AddRow(
                    code,
                    centreName.Length > 0 ? centreName : null,
                    Reference(line.Detail.Order),
                    line.Detail.SubcontractorName,
                    line.Line.Title,
                    line.Detail.Order.Status.ToString(),
                    line.Line.LineTotal,
                    line.Line.PaidToDate);
            }
        }

        return workbook;
    }
}
