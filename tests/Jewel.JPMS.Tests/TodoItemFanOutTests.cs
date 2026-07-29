using System;
using System.Linq;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The fan-out rule behind "one internal email, several to-dos": a triage row with more than one
// assignee — a role, optionally pinned to a named holder — is raised once PER ASSIGNEE — separate
// TODO-#### references, separate tick-boxes — so the QS completing their half never closes the
// site manager's. Both the triage form's item count and the server's row creation run this, so
// these tests pin the promise and the outcome at once.
public sealed class TodoItemFanOutTests
{
    private static TodoItemDraft Draft(string title, params TodoAssignee[] assignees) =>
        new(title, "detail", assignees.Length == 0 ? null : assignees);

    private static TodoAssignee For(Role role, string? personEmail = null) => new(role, personEmail);

    [Fact]
    public void FanOut_raisesOneItemPerAssignee()
    {
        var result = FanOut(Draft("Price the revised joinery", For(Role.QuantitySurveyor), For(Role.ProjectManager)));

        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal("Price the revised joinery", item.Draft.Title));
        Assert.Equal(new Role?[] { Role.QuantitySurveyor, Role.ProjectManager }, result.Select(item => item.AssigneeRole));
    }

    [Fact]
    public void FanOut_keepsUnassignedRowsAsASingleItem()
    {
        var result = FanOut(Draft("Chase the CDM file"));

        var item = Assert.Single(result);
        Assert.Null(item.AssigneeRole);
        Assert.Null(item.AssigneePersonEmail);
    }

    [Fact]
    public void FanOut_treatsAnEmptyAssigneeListAsUnassigned()
    {
        var result = FanOut(new TodoItemDraft("Chase the CDM file", Assignees: Array.Empty<TodoAssignee>()));

        Assert.Null(Assert.Single(result).AssigneeRole);
    }

    [Fact]
    public void FanOut_collapsesADuplicatedAssignee_soNoItemIsRaisedTwice()
    {
        var result = FanOut(Draft("Book the crane", For(Role.SiteManager), For(Role.SiteManager)));

        Assert.Equal(Role.SiteManager, Assert.Single(result).AssigneeRole);
    }

    [Fact]
    public void FanOut_collapsesADuplicatedPin_evenWhenTheEmailCaseDiffers()
    {
        // jane@ and Jane@ are one person, so a hand-rolled request can't raise the same pinned
        // item twice by re-casing the address.
        var result = FanOut(Draft("Book the crane",
            For(Role.SiteManager, "jane@jewelbb.co.uk"),
            For(Role.SiteManager, "Jane@jewelbb.co.uk")));

        var item = Assert.Single(result);
        Assert.Equal(Role.SiteManager, item.AssigneeRole);
        Assert.Equal("jane@jewelbb.co.uk", item.AssigneePersonEmail);
    }

    [Fact]
    public void FanOut_keepsARoleAndTheSameRolePinnedToAPerson_asTwoItems()
    {
        // "The site managers" and "specifically Jane" are two different destinations on purpose:
        // one item for the pool, one for the person.
        var result = FanOut(Draft("Walk the scaffold",
            For(Role.SiteManager),
            For(Role.SiteManager, "jane@jewelbb.co.uk")));

        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal(Role.SiteManager, item.AssigneeRole));
        Assert.Equal(new[] { null, "jane@jewelbb.co.uk" }, result.Select(item => item.AssigneePersonEmail));
    }

    [Fact]
    public void FanOut_holdsOrder_becauseTheItemsAreNumberedInThisSequence()
    {
        var result = FanOut(
            Draft("First", For(Role.ManagingDirector)),
            Draft("Second", For(Role.SiteManager), For(Role.QuantitySurveyor, "qs@jewelbb.co.uk")),
            Draft("Third"));

        Assert.Equal(
            new[] { "First", "Second", "Second", "Third" },
            result.Select(item => item.Draft.Title));
        Assert.Equal(
            new Role?[] { Role.ManagingDirector, Role.SiteManager, Role.QuantitySurveyor, null },
            result.Select(item => item.AssigneeRole));
        Assert.Equal(
            new[] { null, null, "qs@jewelbb.co.uk", null },
            result.Select(item => item.AssigneePersonEmail));
    }

    [Fact]
    public void FanOut_carriesTheRowsDetailAndDueDateOntoEveryItem()
    {
        var due = new DateTimeOffset(2026, 8, 14, 0, 0, 0, TimeSpan.Zero);
        var result = FanOut(
            new TodoItemDraft("Issue the H&S pack", "Before the pour",
                new[] { For(Role.HealthSafetyOfficer), For(Role.SiteManager) }, due));

        Assert.Equal(2, result.Count);
        Assert.All(result, item =>
        {
            Assert.Equal("Before the pour", item.Draft.Notes);
            Assert.Equal(due, item.Draft.DueAt);
        });
    }

    [Fact]
    public void FanOut_returnsNothingForNoDrafts()
    {
        Assert.Empty(TodoItemDrafts.FanOutByAssignee(Array.Empty<TodoItemDraft>()));
    }

    private static System.Collections.Generic.IReadOnlyList<TodoItemFanOut> FanOut(params TodoItemDraft[] drafts) =>
        TodoItemDrafts.FanOutByAssignee(drafts);
}
