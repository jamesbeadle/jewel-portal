using Microsoft.EntityFrameworkCore.Metadata;

namespace Jewel.JPMS.Api.Features.DataProtection;

/// <summary>
/// Every column that can name a person by email, read off the EF model rather than listed by
/// hand: any string property whose name ends in "Email" — ContactEmail, OwnerEmail, the ninety
/// "…ByEmail" stamps, ActorEmail — on any entity. A column added next month is covered the day
/// it is added. Key columns are left out: a row keyed by an email (a sign-in, an access request)
/// is a login, and logins are removed, never rewritten.
/// </summary>
public sealed record PersonColumn(IEntityType Entity, IProperty Property)
{
    public string Table => Entity.ClrType.Name.Replace("Entity", "");
    public string Column => Property.Name;
}

internal static class PersonColumns
{
    private const string EmailSuffix = "Email";

    public static IReadOnlyList<PersonColumn> Of(IModel model) =>
        model.GetEntityTypes()
            .SelectMany(entity => entity.GetProperties().Select(property => new PersonColumn(entity, property)))
            .Where(column => IsAnEmailColumn(column.Property))
            .OrderBy(column => column.Table).ThenBy(column => column.Column)
            .ToList();

    private static bool IsAnEmailColumn(IProperty property)
    {
        var isText = property.ClrType == typeof(string);
        var isNamedForAnEmail = property.Name.EndsWith(EmailSuffix, StringComparison.Ordinal);
        return isText && isNamedForAnEmail && !property.IsKey();
    }
}
