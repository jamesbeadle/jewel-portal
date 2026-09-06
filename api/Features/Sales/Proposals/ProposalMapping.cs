using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Sales.Proposals;

/// <summary>SalesProposalEntity ↔ the models. Options, phases and accepted option ids are JSON
/// columns; unreadable JSON reads as empty rather than failing the page.</summary>
internal static class ProposalMapping
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<ProposalOption> Options(this SalesProposalEntity entity) => Read<ProposalOption>(entity.OptionsJson);
    public static IReadOnlyList<ProposalPhase> Schedule(this SalesProposalEntity entity) => Read<ProposalPhase>(entity.ScheduleJson);
    public static IReadOnlyList<string> AcceptedOptionIds(this SalesProposalEntity entity) => Read<string>(entity.AcceptedOptionIdsJson);

    public static string ToJson<T>(IEnumerable<T> rows) => JsonSerializer.Serialize(rows.ToList(), Json);

    public static SalesProposal ToModel(this SalesProposalEntity entity) =>
        new(
            entity.ProposalId,
            entity.LeadId,
            entity.Version,
            entity.Title,
            entity.Scope,
            entity.BasePrice,
            entity.Options(),
            entity.Schedule(),
            entity.Terms,
            entity.HeroImageId,
            (SalesProposalStatus)entity.Status,
            entity.CreatedByEmail,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.SentAt,
            entity.AcceptedAt,
            entity.AcceptedByName,
            entity.AcceptedByEmail,
            entity.AcceptedOptionIds(),
            entity.AcceptedPrice,
            entity.DeclinedAt,
            entity.DeclineReason);

    public static ProposalView ToView(this SalesProposalEntity entity) =>
        new(
            entity.ProposalId,
            entity.Version,
            entity.Title,
            entity.Scope,
            entity.BasePrice,
            entity.Options(),
            entity.Schedule(),
            entity.Terms,
            entity.HeroImageId,
            (SalesProposalStatus)entity.Status,
            entity.SentAt,
            entity.AcceptedAt,
            entity.AcceptedByName,
            entity.AcceptedOptionIds(),
            entity.AcceptedPrice);

    /// <summary>The proposal the prospect sees: the Accepted one if any, else the Sent one.</summary>
    public static SalesProposalEntity? Live(IEnumerable<SalesProposalEntity> proposals) =>
        proposals.Where(row => row.Status == (int)SalesProposalStatus.Accepted).OrderByDescending(row => row.Version).FirstOrDefault()
        ?? proposals.Where(row => row.Status == (int)SalesProposalStatus.Sent).OrderByDescending(row => row.Version).FirstOrDefault();

    /// <summary>Base price plus the chosen options' differences.</summary>
    public static decimal PriceFor(SalesProposalEntity entity, IEnumerable<string> optionIds)
    {
        var chosen = new HashSet<string>(optionIds, StringComparer.Ordinal);
        return entity.BasePrice + entity.Options().Where(option => chosen.Contains(option.OptionId)).Sum(option => option.PriceDelta);
    }

    private static IReadOnlyList<T> Read<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<T>();
        try { return JsonSerializer.Deserialize<List<T>>(json, Json) ?? new List<T>(); }
        catch (JsonException) { return Array.Empty<T>(); }
    }
}
