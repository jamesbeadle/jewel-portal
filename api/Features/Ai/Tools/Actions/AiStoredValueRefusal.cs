using Jewel.JPMS.Api.Data.StoredValues;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// A connector action whose save was refused because a value did not fit its column answers the
/// model with the sentences, exactly as the page's dialog shows them, and leaves the context
/// clean so the activity log that follows can still be written.
/// </summary>
internal static class AiStoredValueRefusal
{
    public static StoredValuesRejectedException? Find(Exception failure) =>
        failure as StoredValuesRejectedException ?? failure.InnerException as StoredValuesRejectedException;

    public static object AnswerFor(AiToolContext context, StoredValuesRejectedException rejection)
    {
        context.Db.ChangeTracker.Clear();
        return new { ok = false, errors = rejection.Sentences };
    }
}
