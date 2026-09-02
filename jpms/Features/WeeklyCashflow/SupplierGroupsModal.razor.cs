using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Features.WeeklyCashflow;

public partial class SupplierGroupsModal
{
    [Inject] private ICommandSender Commands { get; set; } = default!;
    [Inject] private WeeklyCashflowPlanReadModel Plan { get; set; } = default!;
    [Inject] private IXeroAgedPayablesStore Payables { get; set; } = default!;

    private bool open;
    private bool editorOpen;
    private string? editingGroupId;
    private bool saving;
    private string? error;
    private string formName = "";
    private string memberFilter = "";
    private readonly HashSet<string> formMembers = new(StringComparer.OrdinalIgnoreCase);

    private bool FormLooksComplete =>
        !string.IsNullOrWhiteSpace(formName) && formMembers.Count > 0;

    /// <summary>Opens the dialog on its list of groups — from the toolbar.</summary>
    public void Open()
    {
        editorOpen = false;
        error = null;
        open = true;
        StateHasChanged();
    }

    private void Close() => open = false;

    private void OpenEditor(string? supplierGroupId)
    {
        editingGroupId = supplierGroupId;
        formName = "";
        formMembers.Clear();
        memberFilter = "";
        error = null;
        if (supplierGroupId is not null
            && Plan.Current?.SupplierGroups.FirstOrDefault(group => group.SupplierGroupId == supplierGroupId) is { } group)
        {
            formName = group.Name;
            foreach (var contactName in group.ContactNames) formMembers.Add(contactName);
        }
        editorOpen = true;
    }

    private void ToggleMember(string supplierName)
    {
        if (!formMembers.Remove(supplierName)) formMembers.Add(supplierName);
    }

    /// <summary>Every supplier with an outstanding bill right now, plus the editing group's own
    /// members (a supplier whose bills are momentarily all paid must stay pickable — and
    /// removable), A–Z.</summary>
    private IReadOnlyList<string> SupplierNameChoices()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (Payables.Snapshot() is { } payables)
            foreach (var bill in payables.Bills)
                if (!string.IsNullOrWhiteSpace(bill.ContactName))
                    names.Add(bill.ContactName!.Trim());
        foreach (var member in formMembers) names.Add(member);
        return names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>The name of the OTHER group already holding this supplier, or null. One group
    /// per supplier — a name in two groups would draw its bills twice.</summary>
    private string? GroupHolding(string supplierName)
    {
        var groups = Plan.Current?.SupplierGroups;
        if (groups is null) return null;
        foreach (var group in groups)
        {
            if (group.SupplierGroupId == editingGroupId) continue;
            if (group.ContactNames.Contains(supplierName, StringComparer.OrdinalIgnoreCase)) return group.Name;
        }
        return null;
    }

    private async Task SaveAsync()
    {
        saving = true;
        error = null;
        try
        {
            var saved = await Commands.SendAsync(
                new SaveWeeklyCashflowSupplierGroup(
                    editingGroupId,
                    formName.Trim(),
                    formMembers.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList()),
                CancellationToken.None);
            Plan.Apply(saved);
            editorOpen = false;
        }
        catch (CommandFailedException failure)
        {
            error = failure.Message;
        }
        catch
        {
            error = "Something went wrong saving the group — the red bar at the top has the detail.";
        }
        finally
        {
            saving = false;
        }
    }

    private async Task DeleteAsync()
    {
        if (editingGroupId is not { } supplierGroupId) return;
        saving = true;
        error = null;
        try
        {
            var deleted = await Commands.SendAsync(
                new DeleteWeeklyCashflowSupplierGroup(supplierGroupId), CancellationToken.None);
            Plan.RemoveGroup(deleted.SupplierGroupId);
            editorOpen = false;
        }
        catch (CommandFailedException failure)
        {
            error = failure.Message;
        }
        catch
        {
            error = "Something went wrong deleting the group — the red bar at the top has the detail.";
        }
        finally
        {
            saving = false;
        }
    }
}
