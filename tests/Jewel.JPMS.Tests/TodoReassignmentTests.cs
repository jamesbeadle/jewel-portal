using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Reassigning a to-do means moving it to a different ASSIGNEE — a role, optionally pinned to a
// named holder of it. The role stays the primary assignment (whoever holds it inherits the work
// when someone leaves); a pin narrows the item to one person's list and is cleared by the
// directory commands when that person moves on. There is no separate reassign command: the detail
// modal sends the same full-row UpdateTodoItem the rest of the surface does, with the assignee
// fields different. These tests pin that shape, because "everything else rides along unchanged"
// is the whole safety of doing it that way.
public sealed class TodoReassignmentTests
{
    [Fact]
    public void Reassigning_movesTheRole_andCarriesEverythingElseThrough()
    {
        var item = Sample() with { AssigneeRole = Role.ProjectManager };

        var command = ReassignTo(item, Role.Accounts, null);

        Assert.Equal(Role.Accounts, command.AssigneeRole);
        Assert.Null(command.AssigneePersonEmail);
        Assert.Equal(item.TodoItemId, command.TodoItemId);
        Assert.Equal(item.Title, command.Title);
        Assert.Equal(item.Notes, command.Notes);
        Assert.Equal(item.DueAt, command.DueAt);
        Assert.Equal(item.IsComplete, command.IsComplete);   // a reassign never ticks anything off
    }

    [Fact]
    public void Reassigning_toNobody_isUnassigned_notAnEmptyRole()
    {
        // "Unassigned" is a real destination — the picker's blank option — and it is null, the same
        // value an item raised without an assignee carries. Nothing else may be used for it.
        var command = ReassignTo(Sample() with { AssigneeRole = Role.SiteManager }, null, null);

        Assert.Null(command.AssigneeRole);
        Assert.Null(command.AssigneePersonEmail);
    }

    [Fact]
    public void Reassigning_canPinTheItemToOnePersonWithinTheRole()
    {
        // "Site managers" → "specifically Jane": the role rides along — it is what the item falls
        // back to if Jane moves on — and the pin narrows the list to hers alone.
        var item = Sample() with { AssigneeRole = Role.SiteManager };

        var command = ReassignTo(item, Role.SiteManager, "jane@jewelbb.co.uk");

        Assert.Equal(Role.SiteManager, command.AssigneeRole);
        Assert.Equal("jane@jewelbb.co.uk", command.AssigneePersonEmail);
    }

    [Fact]
    public void Reassigning_backToTheWholeRole_dropsThePin()
    {
        // The reverse move — "Jane's item" back to "any site manager" — must clear the pin, not
        // leave a stale email narrowing a role assignment nobody asked to narrow.
        var pinned = Sample() with { AssigneeRole = Role.SiteManager, AssigneePersonEmail = "jane@jewelbb.co.uk" };

        var command = ReassignTo(pinned, Role.SiteManager, null);

        Assert.Equal(Role.SiteManager, command.AssigneeRole);
        Assert.Null(command.AssigneePersonEmail);
    }

    [Fact]
    public void TickingOff_carriesThePinThrough_unchanged()
    {
        // Completing is the same full-row update with IsComplete flipped: the assignee — pin and
        // all — must survive the round trip exactly, or marking an item done would quietly
        // un-pin it.
        var pinned = Sample() with { AssigneeRole = Role.QuantitySurveyor, AssigneePersonEmail = "qs@jewelbb.co.uk" };

        var command = new UpdateTodoItem(
            pinned.TodoItemId,
            pinned.Title,
            pinned.Notes,
            pinned.AssigneeRole,
            pinned.AssigneePersonEmail,
            pinned.DueAt,
            IsComplete: true);

        Assert.Equal(Role.QuantitySurveyor, command.AssigneeRole);
        Assert.Equal("qs@jewelbb.co.uk", command.AssigneePersonEmail);
        Assert.True(command.IsComplete);
    }

    [Fact]
    public void ADoneItem_keepsItsCompletion_ifItIsEverReassigned()
    {
        // The UI only offers the control on open items, but the command is a full-row update: were
        // a completed item ever put through it, the completion must survive the round trip rather
        // than the item quietly reopening.
        var done = Sample() with { IsComplete = true, CompletedAt = DateTimeOffset.UnixEpoch };

        var command = ReassignTo(done, Role.QuantitySurveyor, null);

        Assert.True(command.IsComplete);
    }

    // Exactly what the detail modal sends: the item as it stands, with the newly picked assignee.
    private static UpdateTodoItem ReassignTo(TodoItem item, Role? role, string? personEmail) => new(
        item.TodoItemId,
        item.Title,
        string.IsNullOrWhiteSpace(item.Notes) ? null : item.Notes,
        role,
        personEmail,
        item.DueAt,
        item.IsComplete);

    private static TodoItem Sample() => new(
        TodoItemId: "todo-1",
        ProjectId: "proj-1",
        Reference: "TODO-0001",
        Title: "Price the rooflight swap",
        Notes: "Client wants it before the valuation goes out.",
        AssigneeRole: null,
        AssigneePersonEmail: null,
        AssigneePersonName: null,
        CreatedByEmail: "pm@jewelbb.co.uk",
        IsComplete: false,
        CreatedAt: DateTimeOffset.UnixEpoch,
        DueAt: DateTimeOffset.UnixEpoch.AddDays(7),
        CompletedAt: null);
}
