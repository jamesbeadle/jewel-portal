using Jewel.JPMS.Contracts.Ai;
using Xunit;

namespace Jewel.JPMS.Tests;

public sealed partial class SkillHistoryTests
{
    private static readonly DateTimeOffset Tenth = new(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Twelfth = new(2026, 9, 12, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fourteenth = new(2026, 9, 14, 9, 0, 0, TimeSpan.Zero);

    private static SkillVersion Version(int number, DateTimeOffset? writtenAt, DateTimeOffset? replacedAt) =>
        new(number, "Doctrine", "When", $"v{number}", Nigel, writtenAt, replacedAt, true, true);

    [Fact]
    public void AWrittenTimeTheStoreDidNotKeep_isReadOffTheReplacementBeforeIt()
    {
        var timeline = SkillVersionTimeline.Of(new[]
        {
            Version(1, null, Twelfth), Version(2, null, Fourteenth), Version(3, Fourteenth, null)
        });

        Assert.Equal(new[] { 3, 2, 1 }, timeline.Select(version => version.Version));
        Assert.Equal(Twelfth, timeline[1].WrittenAt);
        Assert.Null(timeline[2].WrittenAt);
    }

    [Fact]
    public void TheVersionInForce_isTheOneWrittenBeforeAndReplacedAfterTheMoment()
    {
        var timeline = SkillVersionTimeline.Of(new[]
        {
            Version(1, Tenth, Twelfth), Version(2, null, Fourteenth), Version(3, null, null)
        });

        Assert.Null(SkillVersionTimeline.InForceAt(timeline, Tenth.AddDays(-1)));
        Assert.Equal(1, SkillVersionTimeline.InForceAt(timeline, Tenth.AddHours(1))!.Version);
        Assert.Equal(2, SkillVersionTimeline.InForceAt(timeline, Twelfth.AddHours(1))!.Version);
        Assert.Equal(3, SkillVersionTimeline.InForceAt(timeline, Fourteenth.AddDays(3))!.Version);
    }

    [Fact]
    public void TwoTexts_compareLineByLine_keepingWhatTheyShare()
    {
        var comparison = SkillTextComparison.Between("keep\nold rule\nend", "keep\nnew rule\nextra\nend");

        Assert.Equal(new[]
        {
            (SkillTextChange.Same, "keep"), (SkillTextChange.Removed, "old rule"),
            (SkillTextChange.Added, "new rule"), (SkillTextChange.Added, "extra"), (SkillTextChange.Same, "end")
        }, comparison.Select(line => (line.Change, line.Text)));
    }

    [Fact]
    public void IdenticalTexts_haveNoChanges()
    {
        var comparison = SkillTextComparison.Between("a\nb", "a\nb");

        Assert.All(comparison, line => Assert.Equal(SkillTextChange.Same, line.Change));
    }
}
