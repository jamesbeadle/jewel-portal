using static Jewel.JPMS.Features.Cashflow.CashflowDisplay;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Pages;

public partial class CashForecast
{
    // ---- Excel export -------------------------------------------------------
    // Sheet 1: the forecast as rendered — category rows per month plus Later/Undated, net
    // movement, and (directors, bank connected) the closing balance. Sheet 2: the statement
    // rows, signed the way the statement reads. Sheet 3: bank accounts, directors only.
    private ExcelWorkbook? BuildExportWorkbook(bool _)
    {
        var projects = SelectedProjects;
        if (projects.Count == 0) return null;

        var workbook = new ExcelWorkbook();

        // The export button is disabled until TableReady, so the forecast can always be built
        // here — the workbook and the screen come from the same computation.
        if (TableReady && BuildForecast() is { } forecast)
        {
            var columns = new List<ExcelColumn> { new("Row") };
            columns.AddRange(forecast.Axis.Select(month => new ExcelColumn(MonthLabel(month), ExcelFormat.Currency)));
            columns.Add(new ExcelColumn("Later", ExcelFormat.Currency));
            columns.Add(new ExcelColumn("Undated", ExcelFormat.Currency));
            var sheet = workbook.AddSheet("Cash forecast", columns.ToArray());

            void AddCategoryRow(string label, ForecastCategory category, bool cashIn)
            {
                var values = new List<object> { label };
                values.AddRange(forecast.Cells[category].Select(value => (object)(cashIn ? value : -value)));
                values.Add(cashIn ? forecast.Later[category] : -forecast.Later[category]);
                values.Add(cashIn ? forecast.Undated[category] : -forecast.Undated[category]);
                sheet.AddRow(values.ToArray());
            }

            foreach (var row in InRows) AddCategoryRow(row.Label, row.Category, cashIn: true);
            foreach (var row in OutRows) AddCategoryRow(row.Label, row.Category, cashIn: false);

            var movement = new List<object> { "Project movement" };
            movement.AddRange(forecast.ProjectNet.Select(value => (object)value));
            movement.Add(forecast.LaterNet);
            movement.Add(0m);
            sheet.AddRow(movement.ToArray());

            var overheads = new List<object> { "Company overheads" };
            overheads.AddRange(forecast.Axis.Select(month => (object)(-OverheadsFor(month))));
            overheads.Add(0m);
            overheads.Add(0m);
            sheet.AddRow(overheads.ToArray());

            var net = new List<object> { "Net movement" };
            net.AddRange(forecast.Net.Select(value => (object)value));
            net.Add(forecast.LaterNet);
            net.Add(0m);
            sheet.AddRow(net.ToArray());

            if (forecast.Closing.Length > 0)
            {
                var closing = new List<object> { "Closing bank balance" };
                closing.AddRange(forecast.Closing.Select(value => (object)value));
                closing.Add(forecast.Closing[^1] + forecast.LaterNet);
                closing.Add(0m);
                sheet.AddRow(closing.ToArray());
            }
        }

        var statement = workbook.AddSheet("Position to completion",
            new ExcelColumn("Project"),
            new ExcelColumn("Project claim", ExcelFormat.Currency),
            new ExcelColumn("Cash received", ExcelFormat.Currency),
            new ExcelColumn("Retention outstanding", ExcelFormat.Currency),
            new ExcelColumn("Cash allocated", ExcelFormat.Currency),
            new ExcelColumn("Left to claim", ExcelFormat.Currency),
            new ExcelColumn("Cost centre drawdowns", ExcelFormat.Currency),
            new ExcelColumn("Uninvoiced work orders", ExcelFormat.Currency),
            new ExcelColumn("Unpaid purchase invoices", ExcelFormat.Currency),
            new ExcelColumn("Retention release 1", ExcelFormat.Currency),
            new ExcelColumn("Practical completion cashflow", ExcelFormat.Currency),
            new ExcelColumn("Retention release 2", ExcelFormat.Currency),
            new ExcelColumn("Project completion cashflow", ExcelFormat.Currency));

        void AddStatementRow(string name, CashRow row) => statement.AddRow(
            name,
            row.ProjectClaim,
            row.CashReceived,
            row.RetentionOutstanding,
            -row.CashAllocated,
            row.LeftToClaim,
            -row.Drawdown,
            -row.WoLeftToInvoice,
            -row.BillsUnpaid,
            row.Release1,
            row.PracticalCompletionCashflow,
            row.Release2,
            row.ProjectCompletionCashflow);

        foreach (var project in projects)
            AddStatementRow(project.Name, RowFor(project.ProjectId));

        if (projects.Count > 1)
            AddStatementRow("All projects", Totals());

        if (BankSnapshot is { IsConfigured: true, Error: null } bank)
        {
            var banksSheet = workbook.AddSheet("Bank accounts",
                new ExcelColumn("Account"),
                new ExcelColumn("Balance", ExcelFormat.Currency));
            foreach (var account in bank.BankAccounts.OrderByDescending(account => account.Balance))
                banksSheet.AddRow(account.Name, account.Balance);
            banksSheet.AddRow("Total", bank.TotalCash);
        }

        return workbook;
    }

    public void Dispose()
    {
        Projects.OnChanged -= StateHasChanged;
        Summary.OnChanged -= StateHasChanged;
        WorkOrders.OnChanged -= StateHasChanged;
        Lines.OnChanged -= StateHasChanged;
        Claims.OnChanged -= StateHasChanged;
        ClaimEntries.OnChanged -= StateHasChanged;
        Contracts.OnChange -= StateHasChanged;
        Cash.OnChange -= StateHasChanged;
        // The throttle is deliberately NOT disposed: a load still in flight when the user
        // navigates away would Release() a disposed semaphore and fault the abandoned task.
        // An undisposed SemaphoreSlim (no wait-handle use) holds nothing worth reclaiming.
    }
}
