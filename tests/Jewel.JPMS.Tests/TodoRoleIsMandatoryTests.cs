using Jewel.JPMS.Api.Features.Todos.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Every to-do names a role (2026-09-22): the creating and updating commands refuse an item with
// none, Miscellaneous is the conscious "no role owns this" and sits last in the picker, and it is
// a to-do desk no person can hold — never offered as a login role, never granted to an
// administrator's expanded list.
public sealed class TodoRoleIsMandatoryTests
{
    [Fact]
    public void AddingAToDo_withoutARole_isRefused_withTheOneMessage()
    {
        var outcome = new AddTodoItemValidation().Check(new AddTodoItem("p1", "Chase the tiler"));
        Assert.True(outcome.HasFailed);
        Assert.Contains(TodoRoles.RoleIsRequiredMessage, outcome.Errors);
    }

    [Fact]
    public void AddingAGeneralToDo_withoutARole_isRefused()
    {
        var outcome = new AddGeneralTodoItemValidation().Check(new AddGeneralTodoItem("Renew the van insurance"));
        Assert.Contains(TodoRoles.RoleIsRequiredMessage, outcome.Errors);
    }

    [Fact]
    public void UpdatingAToDo_toNoRole_isRefused()
    {
        var outcome = new UpdateTodoItemValidation().Check(new UpdateTodoItem("t1", "Chase the tiler", null, null, null, null, false));
        Assert.Contains(TodoRoles.RoleIsRequiredMessage, outcome.Errors);
    }

    [Fact]
    public void Miscellaneous_isAnAcceptedChoice()
    {
        var outcome = new AddTodoItemValidation().Check(new AddTodoItem("p1", "Sort the skip", AssigneeRole: Role.Miscellaneous));
        Assert.False(outcome.HasFailed);
    }

    [Fact]
    public void ATriageRow_withNobodyOnIt_isRefused()
    {
        var command = new CreateTodoItemsFromMessage("m1", "", new[] { new TodoItemDraft("Book the crane") });
        var outcome = new CreateTodoItemsFromMessageValidation().Check(command);
        Assert.Contains(TodoRoles.RoleIsRequiredMessage, outcome.Errors);
    }

    [Fact]
    public void Miscellaneous_isLastInThePicker_andNeverALoginRole()
    {
        Assert.Equal(Role.Miscellaneous, TodoRoles.AssignableTodoRolesInPickerOrder[^1]);
        Assert.DoesNotContain(Role.Miscellaneous, LoginRoles.All);
        Assert.DoesNotContain(Role.Miscellaneous, UserRoles.Expand(new[] { Role.Admin }));
        Assert.Contains(Role.SalesMarketing, UserRoles.Expand(new[] { Role.Admin }));
    }
}
