using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Rates;

public sealed record ListRatesInLibrary : IQuery<IReadOnlyList<Rate>>;
