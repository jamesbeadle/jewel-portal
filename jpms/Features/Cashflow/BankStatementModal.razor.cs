using System.Globalization;

namespace Jewel.JPMS.Features.Cashflow;

public partial class BankStatementModal
{
    private const string DateInputFormat = "yyyy-MM-dd";

    [Inject] private ICommandSender Commands { get; set; } = default!;
    [Inject] private IXeroCashSummaryStore Cash { get; set; } = default!;

    private bool open;
    private bool saving;
    private string? error;
    private XeroBankAccountBalance? chosen;
    private string formBalance = "";
    private string formDate = "";

    private IReadOnlyList<XeroBankAccountBalance> Accounts =>
        Cash.Snapshot()?.BankAccounts ?? Array.Empty<XeroBankAccountBalance>();

    private static string TodayText => DateTime.Today.ToString(DateInputFormat, CultureInfo.InvariantCulture);

    /// <summary>Opens the dialog on the list of accounts — from the Cash in bank tile.</summary>
    public void Open()
    {
        chosen = null;
        error = null;
        open = true;
        StateHasChanged();
    }

    private void Close() => open = false;

    private void Choose(XeroBankAccountBalance account)
    {
        chosen = account;
        error = null;
        formBalance = account.CashInBank.ToString(CultureInfo.InvariantCulture);
        formDate = TodayText;
    }

    private static string StatementText(XeroBankAccountBalance account)
    {
        var statement = account.Statement;
        if (statement is null) return $"No statement balance — Xero's balance {Money(account.Balance)}";
        var source = statement.Source == BankStatementSource.Keyed ? $"keyed by {statement.KeyedByEmail}" : "from Xero";
        return $"Statement {statement.StatementDate.UtcDateTime:d MMM yyyy}, {source} · Xero's balance {Money(account.Balance)}";
    }

    private KeyBankStatementBalance? BuildCommand()
    {
        if (chosen is null) return null;
        if (!decimal.TryParse(formBalance, NumberStyles.Number, CultureInfo.InvariantCulture, out var balance)) return null;
        if (!DateTime.TryParseExact(formDate, DateInputFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return null;
        var statementDate = new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Utc), TimeSpan.Zero);
        return new KeyBankStatementBalance(chosen.AccountId, chosen.Name, balance, statementDate);
    }

    private async Task SaveAsync()
    {
        var command = BuildCommand();
        if (command is null) { error = "Check the balance and the date — one of them doesn't read."; return; }

        saving = true;
        error = null;
        try
        {
            await Commands.SendAsync(command, CancellationToken.None);
            await Cash.RefreshAsync();
            chosen = null;
        }
        catch (CommandFailedException failure)
        {
            error = failure.Message;
        }
        catch
        {
            error = "Something went wrong saving the statement balance — the red bar at the top has the detail.";
        }
        finally
        {
            saving = false;
        }
    }
}
