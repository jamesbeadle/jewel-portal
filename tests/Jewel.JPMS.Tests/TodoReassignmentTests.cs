using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Reassigning a to-do means moving it to a different ROLE — items are never assigned to a person,
// so that whoever holds the role inherits the work and nothing needs re-assigning when someone
// leaves. There is no separate reassign command: the detail modal sends the same full-row
// UpdateTodoItem the rest of the surface does, with one field different. These tests pin that
// shape, because "everything else rides along unchanged" is the whole safety of doing it that way.
public sealed class TodoReassignmentTests
{
    [Fact]
    public void Reassigning_movesTheRole_andCarriesEverythingElseThrough()
    {
        var item = Sample() with { AssigneeRole = Role.ProjectManager };

        var command = ReassignTo(item, Role.Accounts);

        Assert.Equal(Role.Accounts, command.AssigneeRole);
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
        var command = ReassignTo(Sample() with { AssigneeRole = Role.SiteManager }, null);

        Assert.Null(command.AssigneeRole);
    }

    [Fact]
    public void ADoneItem_keepsItsCompletion_ifItIsEverReassigned()
    {
        // The UI only offers the control on open items, but the command is a full-row update: were
        // a completed item ever put through it, the completion must survive the round trip rather
        // than the item quietly reopening.
        var done = Sample() with { IsComplete = true, CompletedAt = DateTimeOffset.UnixEpoch };

        var command = ReassignTo(done, Role.QuantitySurveyor);

        Assert.True(command.IsComplete);
    }

    // Exactly what the detail modal sends: the item as it stands, with the newly picked role.
    private static UpdateTodoItem ReassignTo(TodoItem item, Role? role) => new(
        item.TodoItemId,
        item.Title,
        string.IsNullOrWhiteSpace(item.Notes) ? null : item.Notes,
        role,
        item.DueAt,
        item.IsComplete);

    private static TodoItem Sample() => new(
        TodoItemId: "todo-1",
        ProjectId: "proj-1",
        Reference: "TODO-0001",
        Title: "Price the rooflight swap",
        Notes: "Client wants it before the valuation goes out.",
        AssigneeRole: null,
        CreatedByEmail: "pm@jewelbb.co.uk",
        IsComplete: false,
        CreatedAt: DateTimeOffset.UnixEpoch,
        DueAt: DateTimeOffset.UnixEpoch.AddDays(7),
        CompletedAt: null);
}
