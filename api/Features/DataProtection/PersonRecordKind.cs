using System.Linq.Expressions;
using Jewel.JPMS.Contracts.DataProtection;

namespace Jewel.JPMS.Api.Features.DataProtection;

/// <summary>
/// One kind of row that IS a person — a client's primary contact, a person at an architect, a
/// lead, a worker — described once: which column holds their email, how the row is titled, the
/// details it holds for the export, and what erasing it means for that row. The catalogue of
/// kinds is <see cref="PersonRecordKinds"/>.
/// </summary>
internal interface IPersonRecordKind
{
    Task<IReadOnlyList<PersonRecord>> FindAsync(JpmsContext context, string email, CancellationToken cancellationToken);
    Task<int> EraseAsync(JpmsContext context, string email, string pseudonym, CancellationToken cancellationToken);
}

internal sealed class PersonRecordKind<TEntity> : IPersonRecordKind where TEntity : class
{
    private readonly string kind;
    private readonly Expression<Func<TEntity, string?>> emailOf;
    private readonly Func<TEntity, string> idOf;
    private readonly Func<TEntity, string> titleOf;
    private readonly Func<TEntity, IEnumerable<PersonRecordField?>> fieldsOf;
    private readonly Action<TEntity, string> erase;

    public PersonRecordKind(
        string kind,
        Expression<Func<TEntity, string?>> emailOf,
        Func<TEntity, string> idOf,
        Func<TEntity, string> titleOf,
        Func<TEntity, IEnumerable<PersonRecordField?>> fieldsOf,
        Action<TEntity, string> erase)
    {
        this.kind = kind;
        this.emailOf = emailOf;
        this.idOf = idOf;
        this.titleOf = titleOf;
        this.fieldsOf = fieldsOf;
        this.erase = erase;
    }

    public async Task<IReadOnlyList<PersonRecord>> FindAsync(JpmsContext context, string email, CancellationToken cancellationToken)
    {
        var rows = await RowsAsync(context, email, cancellationToken);
        return rows.Select(row => new PersonRecord(kind, idOf(row), titleOf(row), FieldsOf(row))).ToList();
    }

    public async Task<int> EraseAsync(JpmsContext context, string email, string pseudonym, CancellationToken cancellationToken)
    {
        var rows = await RowsAsync(context, email, cancellationToken);
        foreach (var row in rows) erase(row, pseudonym);
        return rows.Count;
    }

    private Task<List<TEntity>> RowsAsync(JpmsContext context, string email, CancellationToken cancellationToken)
    {
        var isTheirs = Expression.Lambda<Func<TEntity, bool>>(
            Expression.Equal(emailOf.Body, Expression.Constant(email, typeof(string))), emailOf.Parameters);
        return context.Set<TEntity>().Where(isTheirs).ToListAsync(cancellationToken);
    }

    private IReadOnlyList<PersonRecordField> FieldsOf(TEntity row) =>
        fieldsOf(row).Where(field => field is not null).Select(field => field!).ToList();
}

internal static class PersonRecordFields
{
    public const string Name = "Name";

    public static PersonRecordField? Of(string label, string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : new PersonRecordField(label, value.Trim());
}
