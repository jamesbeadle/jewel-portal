using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Jewel.JPMS.Api.Data.StoredValues;

/// <summary>
/// How a record and a field are named to a person: "ValuationLineItemEntity" reads as
/// "Valuation line item", "RejectionReason" as "Rejection reason".
/// </summary>
public static partial class StoredValueNames
{
    private const string EntitySuffix = "Entity";

    public static string Of(IEntityType record) => Words(TypeNameWithoutSuffix(record.ClrType.Name));

    public static string Of(IProperty column) => Words(column.Name);

    private static string TypeNameWithoutSuffix(string typeName) =>
        typeName.EndsWith(EntitySuffix, StringComparison.Ordinal) ? typeName[..^EntitySuffix.Length] : typeName;

    private static string Words(string pascalCase)
    {
        var words = WordBoundary().Split(pascalCase).Where(word => word.Length > 0).ToList();
        var first = words[0];
        var rest = words.Skip(1).Select(LowerUnlessAcronym);
        return string.Join(" ", rest.Prepend(first));
    }

    private static string LowerUnlessAcronym(string word) =>
        word.All(char.IsUpper) ? word : word.ToLowerInvariant();

    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex WordBoundary();
}
