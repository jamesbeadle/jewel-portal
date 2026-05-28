using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Cvr;

public sealed record ListPrelimEntriesForItem(string PrelimItemId) : IQuery<IReadOnlyList<PrelimForecastEntry>>;
