using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>The current week as the day read carries it: Monday to today, one row per assigned site
/// per working day, so today's card finds its logged day and the worker sees what they have and
/// have not filed before Friday. Earlier weeks and whole months are read by <see cref="MyDayDays"/>.</summary>
public sealed class MyDayWeek
{
    private readonly MyDayDays days;
    public MyDayWeek(JpmsContext context) { days = new MyDayDays(context); }

    public Task<IReadOnlyList<MyWeekDay>> ForAsync(
        WorkerEntity worker, string email, IReadOnlyList<MyLabourProject> cards, DateTimeOffset today, CancellationToken cancellationToken) =>
        days.BetweenAsync(worker, email, SitesOf(cards), LabourWeeks.MondayOf(today), today, cancellationToken);

    public static IReadOnlyList<AssignedProject> SitesOf(IReadOnlyList<MyLabourProject> cards) =>
        cards.Select(card => new AssignedProject(card.ProjectId, card.ProjectName)).ToList();
}
