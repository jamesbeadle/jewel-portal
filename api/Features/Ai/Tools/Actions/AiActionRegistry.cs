using System.Reflection;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// Every action the gateway offers, assembled once at boot from the <see cref="IAiActionSource"/>
/// implementations in this assembly (one per feature area). Construction ASSERTS the whole
/// registry the way AiRegistryDriftCheck asserts the tool catalogue: unique names, resolvable
/// command constructors, stamp names that really are constructor parameters, and an Allows/Check
/// on every gate class — so a mis-declared action kills the app at startup, never a user's call.
/// </summary>
internal static class AiActionRegistry
{
    private static readonly Lazy<IReadOnlyList<AiAction>> Cache = new(BuildAndAssert);

    public static IReadOnlyList<AiAction> All => Cache.Value;

    public static AiAction? Find(string name) =>
        All.FirstOrDefault(action => string.Equals(action.Name, name, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<AiAction> BuildAndAssert()
    {
        var actions = typeof(AiActionRegistry).Assembly.GetTypes()
            .Where(type => typeof(IAiActionSource).IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false })
            .Select(type => (IAiActionSource)Activator.CreateInstance(type)!)
            .SelectMany(source => source.Build())
            .OrderBy(action => action.Area).ThenBy(action => action.Name)
            .ToList();

        var duplicate = actions.GroupBy(action => action.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"AiActionRegistry: duplicate action name '{duplicate.Key}'.");

        foreach (var action in actions)
        {
            var parameters = AiActionSchema.Constructor(action.CommandType).GetParameters()
                .Select(parameter => parameter.Name!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var stamp in action.EmailStamps.Concat(action.NameStamps))
            {
                if (!parameters.Contains(stamp))
                    throw new InvalidOperationException(
                        $"AiActionRegistry: '{action.Name}' stamps '{stamp}', which is not a parameter of {action.CommandType.Name}.");
            }

            // Overload SELECTION is asserted, not just existence: shared gate classes carry many
            // typed Allows/Check overloads gating different role sets, and picking the wrong one
            // would enforce the wrong gate (found in review, 2026-08-28).
            if (AiActionExecutor.FindAllows(action.AuthorisationType, action.CommandType) is null)
                throw new InvalidOperationException(
                    $"AiActionRegistry: '{action.Name}' — {action.AuthorisationType.Name} has no Allows overload for {action.CommandType.Name}.");
            if (action.ValidationType is not null
                && AiActionExecutor.FindCheck(action.ValidationType, action.CommandType) is null)
                throw new InvalidOperationException(
                    $"AiActionRegistry: '{action.Name}' — {action.ValidationType.Name} has no Check/CheckAsync overload for {action.CommandType.Name}.");
        }

        return actions;
    }
}
