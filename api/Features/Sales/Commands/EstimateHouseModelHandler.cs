using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Commands;

/// <summary>Stores the estimate's 3D model (2026-09-16): the definition replaced whole, compact,
/// with the sheets and revision it was read from.</summary>
public sealed class SetEstimateHouseModelHandler : ICommandHandler<SetEstimateHouseModel, LeadEstimate>
{
    private readonly JpmsContext context;
    public SetEstimateHouseModelHandler(JpmsContext context) { this.context = context; }

    private IQueryable<LeadEstimateLineEntity> LinesOf(string estimateId) =>
        context.LeadEstimateLines.AsNoTracking().Where(line => line.EstimateId == estimateId);

    public async Task<LeadEstimate> HandleAsync(SetEstimateHouseModel command, CancellationToken cancellationToken)
    {
        var entity = await EstimateLookup.FindAsync(context.LeadEstimates, command.EstimateId, cancellationToken)
            ?? throw new InvalidOperationException($"Estimate {command.EstimateId} not found.");
        var status = (EstimateStatus)entity.Status;
        if (!status.IsOpen())
            throw new InvalidOperationException($"{entity.Reference} is {status.DisplayName()} — it is history and its model cannot be changed.");

        var now = DateTimeOffset.UtcNow;
        entity.HouseModelJson = JsonSerializer.Serialize(command.Model);
        entity.HouseModelSource = command.Source.Trim();
        entity.HouseModelSetAt = now;

        EstimateTimeline.Write(context, entity, $"Estimate {entity.Reference} 3D model set — {HouseModelShape.Describe(command.Model)}, from {entity.HouseModelSource}", command.ChangedByEmail, now);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel(await LinesOf(entity.EstimateId).ToListAsync(cancellationToken));
    }
}

/// <summary>What the definition holds, read from the JSON — its shape belongs to the viewer and
/// the jpms-house-model skill, never to a typed model here.</summary>
internal static class HouseModelShape
{
    public const int MaxJsonCharacters = 512_000;

    public static int CountOf(JsonElement model, string property)
    {
        if (!model.TryGetProperty(property, out var value)) return 0;
        return value.ValueKind == JsonValueKind.Array ? value.GetArrayLength() : 0;
    }

    public static bool HasName(JsonElement model)
    {
        if (!model.TryGetProperty("name", out var name)) return false;
        if (name.ValueKind != JsonValueKind.String) return false;
        return !string.IsNullOrWhiteSpace(name.GetString());
    }

    public static string Describe(JsonElement model)
    {
        var parts = new List<string> { Plural(CountOf(model, "blocks"), "block") };
        var openings = CountOf(model, "openings");
        if (openings > 0) parts.Add(Plural(openings, "opening"));
        var elements = CountOf(model, "elements");
        if (elements > 0) parts.Add(Plural(elements, "element"));
        var stages = model.TryGetProperty("programme", out var programme) ? CountOf(programme, "stages") : 0;
        if (stages > 0) parts.Add(Plural(stages, "stage"));
        return string.Join(", ", parts);
    }

    private static string Plural(int count, string noun) => $"{count} {noun}{(count == 1 ? "" : "s")}";
}
