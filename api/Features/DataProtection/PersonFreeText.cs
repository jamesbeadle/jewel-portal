using System.Reflection;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.DataProtection;

namespace Jewel.JPMS.Api.Features.DataProtection;

/// <summary>
/// The free-text columns that quote a person in passing — an audit sentence, an agent summary,
/// a lead's timeline, a message body — where their email or name sits inside other words. The
/// email is replaced by the pseudonym and each name by the erased marker, in place, so the
/// sentence still reads and the record still says what happened.
/// </summary>
internal sealed class PersonFreeText
{
    private static readonly MethodInfo RewriteMethod = typeof(PersonFreeText).GetMethod(nameof(RewriteAsync), BindingFlags.NonPublic | BindingFlags.Static)!;
    private const int ShortestNameWorthReplacing = 4;

    private static readonly IReadOnlyList<(Type Entity, string Column)> Columns = new (Type, string)[]
    {
        (typeof(AuditEventEntity), nameof(AuditEventEntity.Detail)),
        (typeof(AgentActivityEntity), nameof(AgentActivityEntity.Summary)),
        (typeof(LeadActivityEntity), nameof(LeadActivityEntity.Summary)),
        (typeof(RequestMessageEntity), nameof(RequestMessageEntity.Body)),
        (typeof(KpiEmailEntity), nameof(KpiEmailEntity.Note)),
        (typeof(ImagineRoundEntity), nameof(ImagineRoundEntity.Brief))
    };

    private readonly JpmsContext context;
    public PersonFreeText(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<PersonMention>> RewriteAsync(
        string email, string pseudonym, IReadOnlyCollection<string> names, CancellationToken cancellationToken)
    {
        var namesWorthReplacing = names
            .Where(name => name.Trim().Length >= ShortestNameWorthReplacing)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var rewritten = new List<PersonMention>();
        foreach (var (entity, column) in Columns)
        {
            var count = await ClosedOverEntity.Invoke<int>(RewriteMethod, entity, context, column, email, pseudonym, namesWorthReplacing, cancellationToken);
            var wasQuoted = count > 0;
            if (wasQuoted)
                rewritten.Add(new PersonMention(entity.Name.Replace("Entity", ""), column, count));
        }
        return rewritten;
    }

    private static async Task<int> RewriteAsync<TEntity>(
        JpmsContext context, string column, string email, string pseudonym, IReadOnlyList<string> names, CancellationToken cancellationToken)
        where TEntity : class
    {
        var rows = await context.Set<TEntity>()
            .Where(row => EF.Property<string>(row, column) != null && EF.Property<string>(row, column).Contains(email))
            .ToListAsync(cancellationToken);
        foreach (var row in rows)
        {
            var property = context.Entry(row).Property(column);
            property.CurrentValue = Redacted((string)property.CurrentValue!, email, pseudonym, names);
        }
        return rows.Count;
    }

    private static string Redacted(string text, string email, string pseudonym, IReadOnlyList<string> names)
    {
        var redacted = text.Replace(email, pseudonym, StringComparison.OrdinalIgnoreCase);
        foreach (var name in names) redacted = redacted.Replace(name, PersonPseudonym.ErasedName, StringComparison.OrdinalIgnoreCase);
        return redacted;
    }
}
