using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.WeeklyCashflow;

namespace Jewel.JPMS.Pages;

public partial class WeeklyCashflow
{
    // ---- The item dialog ----------------------------------------------------

    private void OpenAddDialog()
    {
        editingItemId = null;
        formName = "";
        formCategory = WeeklyCashflowCategory.Subcontractor;
        formAmount = "";
        formRecurrence = WeeklyCashflowRecurrence.OneOff;
        formFirstDue = today.UtcDateTime.ToString("yyyy-MM-dd");
        formLastDue = "";
        formNotes = "";
        dialogError = null;
        itemDialogOpen = true;
    }

    private void OpenEditDialog(string itemId)
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
        dialogError = null;
        itemDialogOpen = true;
    }

    private void CloseItemDialog() => itemDialogOpen = false;

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

    private async Task SaveItemAsync()
    {
        if (ReadForm() is not { } details)
        {
            dialogError = "Check the amount and dates — one of them doesn't parse.";
            return;
        }
        isSavingItem = true;
        dialogError = null;
        try
        {
            var saved = editingItemId is { } itemId
                ? await Commands.SendAsync(new UpdateWeeklyCashflowItem(itemId, details), CancellationToken.None)
                : await Commands.SendAsync(new CreateWeeklyCashflowItem(details), CancellationToken.None);
            Plan.Apply(saved);
            itemDialogOpen = false;
        }
        catch (CommandFailedException failure)
        {
            dialogError = failure.Message;
        }
        catch
        {
            dialogError = "Something went wrong saving the item — the red bar at the top has the detail.";
        }
        finally
        {
            isSavingItem = false;
        }
    }

    private async Task ArchiveItemAsync()
    {
        if (editingItemId is not { } itemId) return;
        isSavingItem = true;
        dialogError = null;
        try
        {
            var archived = await Commands.SendAsync(new ArchiveWeeklyCashflowItem(itemId), CancellationToken.None);
            Plan.Apply(archived);
            itemDialogOpen = false;
        }
        catch (CommandFailedException failure)
        {
            dialogError = failure.Message;
        }
        catch
        {
            dialogError = "Something went wrong archiving the item — the red bar at the top has the detail.";
        }
        finally
        {
            isSavingItem = false;
        }
    }

    // ---- Export -------------------------------------------------------------
}
