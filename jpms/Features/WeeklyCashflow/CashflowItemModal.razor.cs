using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Features.WeeklyCashflow;

public partial class CashflowItemModal
{
    [Inject] private ICommandSender Commands { get; set; } = default!;
    [Inject] private WeeklyCashflowPlanReadModel Plan { get; set; } = default!;

    private bool open;
    private string? editingItemId;
    private bool saving;
    private string? error;
    private string formName = "";
    private WeeklyCashflowCategory formCategory = WeeklyCashflowCategory.Subcontractor;
    private string formAmount = "";
    private WeeklyCashflowRecurrence formRecurrence = WeeklyCashflowRecurrence.OneOff;
    private string formFirstDue = "";
    private string formLastDue = "";
    private string formNotes = "";

    private bool FormLooksComplete =>
        !string.IsNullOrWhiteSpace(formName)
        && decimal.TryParse(formAmount, out var amount) && amount > 0m
        && DateTime.TryParse(formFirstDue, out _);

    /// <summary>Opens the dialog empty, the first due date seeded to the page's "as of" day.</summary>
    public void OpenAdd(DateTimeOffset today)
    {
        editingItemId = null;
        formName = "";
        formCategory = WeeklyCashflowCategory.Subcontractor;
        formAmount = "";
        formRecurrence = WeeklyCashflowRecurrence.OneOff;
        formFirstDue = today.UtcDateTime.ToString("yyyy-MM-dd");
        formLastDue = "";
        formNotes = "";
        error = null;
        open = true;
        StateHasChanged();
    }

    /// <summary>Opens the dialog over an existing item — from its row in the grid.</summary>
    public void OpenEdit(string itemId)
    {
        var item = Plan.Current?.Items.FirstOrDefault(row => row.WeeklyCashflowItemId == itemId);
        if (item is null) return;
        editingItemId = item.WeeklyCashflowItemId;
        formName = item.Name;
        formCategory = item.Category;
        formAmount = item.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        formRecurrence = item.Recurrence;
        formFirstDue = item.FirstDueOn.UtcDateTime.ToString("yyyy-MM-dd");
        formLastDue = item.LastDueOn?.UtcDateTime.ToString("yyyy-MM-dd") ?? "";
        formNotes = item.Notes ?? "";
        error = null;
        open = true;
        StateHasChanged();
    }

    private void Close() => open = false;

    private WeeklyCashflowItemDetails? ReadForm()
    {
        if (!decimal.TryParse(formAmount, out var amount)) return null;
        if (!DateTime.TryParse(formFirstDue, out var firstDue)) return null;
        DateTimeOffset? lastDue = null;
        if (formRecurrence != WeeklyCashflowRecurrence.OneOff
            && !string.IsNullOrWhiteSpace(formLastDue)
            && DateTime.TryParse(formLastDue, out var lastDueDate))
        {
            lastDue = new DateTimeOffset(DateTime.SpecifyKind(lastDueDate.Date, DateTimeKind.Utc), TimeSpan.Zero);
        }
        return new WeeklyCashflowItemDetails(
            formName.Trim(),
            formCategory,
            amount,
            formRecurrence,
            new DateTimeOffset(DateTime.SpecifyKind(firstDue.Date, DateTimeKind.Utc), TimeSpan.Zero),
            lastDue,
            string.IsNullOrWhiteSpace(formNotes) ? null : formNotes.Trim());
    }

    private async Task SaveAsync()
    {
        if (ReadForm() is not { } details)
        {
            error = "Check the amount and dates — one of them doesn't parse.";
            return;
        }
        saving = true;
        error = null;
        try
        {
            var saved = editingItemId is { } itemId
                ? await Commands.SendAsync(new UpdateWeeklyCashflowItem(itemId, details), CancellationToken.None)
                : await Commands.SendAsync(new CreateWeeklyCashflowItem(details), CancellationToken.None);
            Plan.Apply(saved);
            open = false;
        }
        catch (CommandFailedException failure)
        {
            error = failure.Message;
        }
        catch
        {
            error = "Something went wrong saving the item — the red bar at the top has the detail.";
        }
        finally
        {
            saving = false;
        }
    }

    private async Task ArchiveAsync()
    {
        if (editingItemId is not { } itemId) return;
        saving = true;
        error = null;
        try
        {
            var archived = await Commands.SendAsync(new ArchiveWeeklyCashflowItem(itemId), CancellationToken.None);
            Plan.Apply(archived);
            open = false;
        }
        catch (CommandFailedException failure)
        {
            error = failure.Message;
        }
        catch
        {
            error = "Something went wrong archiving the item — the red bar at the top has the detail.";
        }
        finally
        {
            saving = false;
        }
    }
}
