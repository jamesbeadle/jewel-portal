using System.Reflection;
using Jewel.JPMS.Contracts.DataProtection;

namespace Jewel.JPMS.Api.Features.DataProtection;

/// <summary>
/// Counts and rewrites the rows that merely name a person — every <see cref="PersonColumn"/>
/// holding their email. The entity type is known only at run time, so one generic reader and
/// one generic writer are closed over each column by reflection; the rows themselves are read
/// and changed through the ordinary change tracker, so the same code runs against SQL Server
/// and the tests' in-memory store alike.
/// </summary>
internal sealed class PersonMentions
{
    private static readonly MethodInfo CountMethod = typeof(PersonMentions).GetMethod(nameof(CountAsync), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo RewriteMethod = typeof(PersonMentions).GetMethod(nameof(RewriteAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private readonly JpmsContext context;
    private readonly IReadOnlyList<PersonColumn> columns;

    public PersonMentions(JpmsContext context)
    {
        this.context = context;
        columns = PersonColumns.Of(context.Model);
    }

    public async Task<IReadOnlyList<PersonMention>> CountAsync(string email, CancellationToken cancellationToken)
    {
        var mentions = new List<PersonMention>();
        foreach (var column in columns)
        {
            var count = await Invoke<int>(CountMethod, column, email, cancellationToken);
            var isMentioned = count > 0;
            if (isMentioned)
                mentions.Add(new PersonMention(column.Table, column.Column, count));
        }
        return mentions;
    }

    public async Task<IReadOnlyList<PersonMention>> RewriteAsync(string email, string pseudonym, CancellationToken cancellationToken)
    {
        var rewritten = new List<PersonMention>();
        foreach (var column in columns)
        {
            var count = await Invoke<int>(RewriteMethod, column, email, pseudonym, cancellationToken);
            var wasMentioned = count > 0;
            if (wasMentioned)
                rewritten.Add(new PersonMention(column.Table, column.Column, count));
        }
        return rewritten;
    }

    private Task<T> Invoke<T>(MethodInfo method, PersonColumn column, params object[] arguments) =>
        ClosedOverEntity.Invoke<T>(method, column.Entity.ClrType, context, column.Column, arguments);

    private static Task<int> CountAsync<TEntity>(JpmsContext context, string column, string email, CancellationToken cancellationToken)
        where TEntity : class =>
        context.Set<TEntity>().CountAsync(row => EF.Property<string>(row, column) == email, cancellationToken);

    private static async Task<int> RewriteAsync<TEntity>(JpmsContext context, string column, string email, string pseudonym, CancellationToken cancellationToken)
        where TEntity : class
    {
        var rows = await context.Set<TEntity>()
            .Where(row => EF.Property<string>(row, column) == email)
            .ToListAsync(cancellationToken);
        foreach (var row in rows) context.Entry(row).Property(column).CurrentValue = pseudonym;
        return rows.Count;
    }
}
