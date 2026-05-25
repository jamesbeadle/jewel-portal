using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IRateLibrary
{
    IReadOnlyList<Rate> All();

    Rate? Find(string rateId);

    Rate Upsert(Rate rate);

    IReadOnlyList<Rate> Stale(int dayThreshold);

    event Action? OnChange;
}
