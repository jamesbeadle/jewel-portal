using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.WeeklyCashflow;

namespace Jewel.JPMS.Pages;

public partial class WeeklyCashflow
{
    // ---- The supplier groups dialog -----------------------------------------

    private void OpenGroupsDialog()
    {
        groupEditorOpen = false;
        groupsDialogError = null;
        groupsDialogOpen = true;
    }

    private void CloseGroupsDialog() => groupsDialogOpen = false;

    private void OpenGroupEditor(string? supplierGroupId)
    {
        editingGroupId = supplierGroupId;
        groupFormName = "";
        groupFormMembers.Clear();
        groupMemberFilter = "";
        groupsDialogError = null;
        if (supplierGroupId is not null
            && Plan.Current?.SupplierGroups.FirstOrDefault(group => group.SupplierGroupId == supplierGroupId) is { } group)
        {
            groupFormName = group.Name;
            foreach (var contactName in group.ContactNames) groupFormMembers.Add(contactName);
        }
        groupEditorOpen = true;
    }

    private void ToggleGroupMember(string supplierName)
    {
        if (!groupFormMembers.Remove(supplierName)) groupFormMembers.Add(supplierName);
    }

    /// <summary>Every supplier with an outstanding bill right now, plus the editing group's own
    /// members (a supplier whose bills are momentarily all paid must stay pickable — and
    /// removable), A–Z.</summary>
    private IReadOnlyList<string> SupplierNameChoices()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (PayablesSnapshot is { } payables)
            foreach (var bill in payables.Bills)
                if (!string.IsNullOrWhiteSpace(bill.ContactName))
                    names.Add(bill.ContactName!.Trim());
        foreach (var member in groupFormMembers) names.Add(member);
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

    private async Task SaveGroupAsync()
    {
        isSavingGroup = true;
        groupsDialogError = null;
        try
        {
            var saved = await Commands.SendAsync(
                new SaveWeeklyCashflowSupplierGroup(
                    editingGroupId,
                    groupFormName.Trim(),
                    groupFormMembers.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList()),
                CancellationToken.None);
            Plan.Apply(saved);
            groupEditorOpen = false;
        }
        catch (CommandFailedException failure)
        {
            groupsDialogError = failure.Message;
        }
        catch
        {
            groupsDialogError = "Something went wrong saving the group — the red bar at the top has the detail.";
        }
        finally
        {
            isSavingGroup = false;
        }
    }

    private async Task DeleteGroupAsync()
    {
        if (editingGroupId is not { } supplierGroupId) return;
        isSavingGroup = true;
        groupsDialogError = null;
        try
        {
            var deleted = await Commands.SendAsync(
                new DeleteWeeklyCashflowSupplierGroup(supplierGroupId), CancellationToken.None);
            Plan.RemoveGroup(deleted.SupplierGroupId);
            groupEditorOpen = false;
        }
        catch (CommandFailedException failure)
        {
            groupsDialogError = failure.Message;
        }
        catch
        {
            groupsDialogError = "Something went wrong deleting the group — the red bar at the top has the detail.";
        }
        finally
        {
            isSavingGroup = false;
        }
    }

}
