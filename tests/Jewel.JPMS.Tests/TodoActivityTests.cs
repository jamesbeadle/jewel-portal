using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Todos;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The to-do timeline's rules (2026-08-25): which lines a full-row update earns, what an email
// tag resolves to, and which kinds move an item to In progress. Pinned because the whole point
// of the timeline is that "I chased it on the 24th" is recorded exactly once, in the right
// words, without the item being marked done.
public sealed class TodoActivityTests
{
    [Fact]
    public void CompletingWritesOneLine_andNothingElseWhenNothingElseChanged()
    {
        var before = Sample();
        var after = Sample();
        after.IsComplete = true;

        var lines = TodoActivitySummaries.ForUpdate(before, after);

        var line = Assert.Single(lines);
        Assert.Equal(TodoActivityKind.Completed, line.Kind);
        Assert.Equal("Marked done", line.Summary);
    }

    [Fact]
    public void ReassigningNamesTheRole_andThePinnedPerson()
    {
        var before = Sample();
        var after = Sample();
        after.AssigneeRole = (int)Role.FinanceDirector;
        after.AssigneePersonEmail = "jeremy@jewelenterprises.co.uk";

        var line = Assert.Single(TodoActivitySummaries.ForUpdate(before, after));

        Assert.Equal(TodoActivityKind.Reassigned, line.Kind);
        Assert.Equal("Reassigned to Finance Director (jeremy@jewelenterprises.co.uk)", line.Summary);
    }

    [Fact]
    public void SeveralChangesAtOnce_areSeveralLines_stateFirst()
    {
        var before = Sample();
        var after = Sample();
        after.IsComplete = false;
        before.IsComplete = true;
        after.DueAt = new DateTimeOffset(2026, 9, 5, 0, 0, 0, TimeSpan.Zero);
        after.Title = "chase Justine again";

        var kinds = TodoActivitySummaries.ForUpdate(before, after).Select(line => line.Kind).ToList();

        Assert.Equal(new[] { TodoActivityKind.Reopened, TodoActivityKind.DueChanged, TodoActivityKind.Edited }, kinds);
    }

    [Fact]
    public void AnUnchangedRow_earnsNoLine()
    {
        Assert.Empty(TodoActivitySummaries.ForUpdate(Sample(), Sample()));
    }

    [Fact]
    public void EmailTags_resolveToItemNumbers_ignoringEveryOtherTag()
    {
        var numbers = TodoEmailActivityRecorder.TodoTagNumbers(new[] { "JPMS/TODO-0083", "JPMS/Client", "JPMS/JBB-2026-005-RFI-012", "jpms/todo-7" });

        Assert.Equal(new[] { 83, 7 }, numbers);
    }

    [Theory]
    [InlineData(TodoActivityKind.Started, true)]
    [InlineData(TodoActivityKind.Chased, true)]
    [InlineData(TodoActivityKind.EmailSent, true)]
    [InlineData(TodoActivityKind.Note, false)]
    [InlineData(TodoActivityKind.Reassigned, false)]
    public void OnlyStartingChasingAndEmailing_moveAnItemToInProgress(TodoActivityKind kind, bool starts)
    {
        Assert.Equal(starts, TodoProgressKinds.StartsTheItem(kind));
    }

    [Fact]
    public void ChaseSummary_carriesTheNote_orStandsAlone()
    {
        Assert.Equal("Chased", TodoActivitySummaries.ChaseSummary("  "));
        Assert.Equal("Chased — emailed Justine for the certificate", TodoActivitySummaries.ChaseSummary("emailed Justine for the certificate "));
    }

    private static TodoItemEntity Sample() => new()
    {
        TodoItemId = "t1",
        Title = "chase Justine for our Certificate and Retention",
        Notes = "",
        AssigneeRole = (int)Role.FinanceDirector,
        AssigneePersonEmail = null,
        DueAt = new DateTimeOffset(2026, 8, 29, 0, 0, 0, TimeSpan.Zero),
        IsComplete = false,
    };
}
