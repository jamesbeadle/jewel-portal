using System;
using System.Linq;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The fan-out rule behind "one internal email, several to-dos": a triage row assigned to more than
// one role is raised once PER ROLE — separate TODO-#### references, separate tick-boxes — so the QS
// completing their half never closes the site manager's. Both the triage form's item count and the
// server's row creation run this, so these tests pin the promise and the outcome at once.
public sealed class TodoItemFanOutTests
{
    private static TodoItemDraft Draft(string title, params Role[] roles) =>
        new(title, "detail", roles.Length == 0 ? null : roles);

    [Fact]
    public void FanOutByRole_raisesOneItemPerRole()
    {
        var result = TodoItemFanOutTests.FanOut(Draft("Price the revised joinery", Role.QuantitySurveyor, Role.ProjectManager));

        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal("Price the revised joinery", item.Draft.Title));
        Assert.Equal(new Role?[] { Role.QuantitySurveyor, Role.ProjectManager }, result.Select(item => item.AssigneeRole));
    }

    [Fact]
    public void FanOutByRole_keepsUnassignedRowsAsASingleItem()
    {
        var result = TodoItemFanOutTests.FanOut(Draft("Chase the CDM file"));

        var item = Assert.Single(result);
        Assert.Null(item.AssigneeRole);
    }

    [Fact]
    public void FanOutByRole_treatsAnEmptyRoleListAsUnassigned()
    {
        var result = TodoItemFanOutTests.FanOut(new TodoItemDraft("Chase the CDM file", AssigneeRoles: Array.Empty<Role>()));

        Assert.Null(Assert.Single(result).AssigneeRole);
    }

    [Fact]
    public void FanOutByRole_collapsesADuplicatedRole_soNoItemIsRaisedTwice()
    {
        var result = TodoItemFanOutTests.FanOut(Draft("Book the crane", Role.SiteManager, Role.SiteManager));

        Assert.Equal(Role.SiteManager, Assert.Single(result).AssigneeRole);
    }

    [Fact]
    public void FanOutByRole_holdsOrder_becauseTheItemsAreNumberedInThisSequence()
    {
        var result = TodoItemFanOutTests.FanOut(
            Draft("First", Role.ManagingDirector),
            Draft("Second", Role.SiteManager, Role.QuantitySurveyor),
            Draft("Third"));

        Assert.Equal(
            new[] { "First", "Second", "Second", "Third" },
            result.Select(item => item.Draft.Title));
        Assert.Equal(
            new Role?[] { Role.ManagingDirector, Role.SiteManager, Role.QuantitySurveyor, null },
            result.Select(item => item.AssigneeRole));
    }

    [Fact]
    public void FanOutByRole_carriesTheRowsDetailAndDueDateOntoEveryItem()
    {
        var due = new DateTimeOffset(2026, 8, 14, 0, 0, 0, TimeSpan.Zero);
        var result = TodoItemFanOutTests.FanOut(
            new TodoItemDraft("Issue the H&S pack", "Before the pour", new[] { Role.HealthSafetyOfficer, Role.SiteManager }, due));

        Assert.Equal(2, result.Count);
        Assert.All(result, item =>
        {
            Assert.Equal("Before the pour", item.Draft.Notes);
            Assert.Equal(due, item.Draft.DueAt);
        });
    }

    [Fact]
    public void FanOutByRole_returnsNothingForNoDrafts()
    {
        Assert.Empty(TodoItemDrafts.FanOutByRole(Array.Empty<TodoItemDraft>()));
    }

    private static System.Collections.Generic.IReadOnlyList<TodoItemFanOut> FanOut(params TodoItemDraft[] drafts) =>
        TodoItemDrafts.FanOutByRole(drafts);
}
