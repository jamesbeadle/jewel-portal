using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.WeeklyCashflow;

namespace Jewel.JPMS.Pages;

public partial class WeeklyCashflow
{
    // ---- Moving -------------------------------------------------------------

    private async Task MoveEntryAsync(WeeklyCashflowEntry entry, int targetIndex)
    {
        var view = BuildView();
        if (targetIndex < 0 || targetIndex >= view.WeekStarts.Count) return;
        await PlaceAsync(entry, view.WeekStarts[targetIndex]);
    }

    private Task ResetEntryAsync(WeeklyCashflowEntry entry) => PlaceAsync(entry, null);

    private async Task PlaceAsync(WeeklyCashflowEntry entry, DateTimeOffset? weekStart)
    {
        if (!movingKeys.Add(entry.PlacementKey)) return;
        moveError = null;
        try
        {
            // The answer is enveloped: a cleared placement comes back as { placement: null }
            // rather than an empty 204 the JSON read would choke on (JPMS-31996D).
            var answer = await Commands.SendAsync(
                new PlaceWeeklyCashflowEntry(entry.PlacementKey, weekStart), CancellationToken.None);
            // The server's answer is the truth the whole grid re-derives from — no refetch needed.
            Plan.Apply(entry.PlacementKey, answer.Placement);
        }
        catch (CommandFailedException failure)
        {
            moveError = failure.Message;
        }
        catch
        {
            // Already reported to the error toast with a reference; the grid simply stays put.
        }
        finally
        {
            movingKeys.Remove(entry.PlacementKey);
        }
    }

    private async Task MoveGroupCellAsync(GroupSlice slice, int cellIndex, int targetIndex)
    {
        var view = BuildView();
        if (targetIndex < 0 || targetIndex >= view.WeekStarts.Count) return;
        var members = slice.Entries.Where(entry => entry.WeekIndex == cellIndex).ToList();
        await MoveGroupMembersAsync(slice, members, view.WeekStarts[targetIndex]);
    }

    private Task ResetGroupCellAsync(GroupSlice slice, int cellIndex) =>
        MoveGroupMembersAsync(
            slice,
            slice.Entries.Where(entry => entry.WeekIndex == cellIndex && entry.Moved).ToList(),
            null);

    /// <summary>Moves a group cell's bills one by one — each is the same shared placement as
    /// moving the bill by hand, so colleagues see one plan however it was driven. The first
    /// failure stops the batch; whatever already moved stays moved, and the grid shows it.</summary>
    private async Task MoveGroupMembersAsync(GroupSlice slice, IReadOnlyList<WeeklyCashflowEntry> members, DateTimeOffset? weekStart)
    {
        if (members.Count == 0 || !movingGroupIds.Add(slice.Group.SupplierGroupId)) return;
        try
        {
            foreach (var member in members)
            {
                await PlaceAsync(member, weekStart);
                if (moveError is not null) break;
            }
        }
        finally
        {
            movingGroupIds.Remove(slice.Group.SupplierGroupId);
        }
    }

    // ---- Excluding ----------------------------------------------------------

    private async Task SetExclusionAsync(string placementKey, bool excluded)
    {
        if (!excludingKeys.Add(placementKey)) return;
        moveError = null;
        try
        {
            // Enveloped like placements: a lifted exclusion answers { exclusion: null },
            // never an empty 204 the JSON read would choke on.
            var answer = await Commands.SendAsync(
                new SetWeeklyCashflowExclusion(placementKey, excluded), CancellationToken.None);
            Plan.ApplyExclusion(placementKey, answer.Exclusion);
        }
        catch (CommandFailedException failure)
        {
            moveError = failure.Message;
        }
        catch
        {
            // Already reported to the error toast with a reference; the grid simply stays put.
        }
        finally
        {
            excludingKeys.Remove(placementKey);
        }
    }

}
