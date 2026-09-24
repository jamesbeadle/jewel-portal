using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Jewel.JPMS.Api.Data.StoredValues;

/// <summary>
/// Every value a save is about to write, checked against the column the model says will hold
/// it: text no longer than its length, a number inside its precision, a required value present.
/// The model is the one source of the limits — the same [MaxLength] and precision the migrations
/// were built from — so a column added tomorrow is checked the day it is added.
/// </summary>
public static class StoredValueChecks
{
    private const int LongestBoundedNvarchar = 4000;
    private const int DefaultDecimalPrecision = 18;
    private const int DefaultDecimalScale = 4;
    private const int WholeDigitsDecimalAlwaysHolds = 28;

    public static IReadOnlyList<StoredValueProblem> ProblemsIn(IEnumerable<EntityEntry> entries) =>
        entries
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .SelectMany(ProblemsInEntry)
            .ToList();

    private static IEnumerable<StoredValueProblem> ProblemsInEntry(EntityEntry entry) =>
        entry.Properties
            .Where(property => entry.State == EntityState.Added || property.IsModified)
            .Select(property => ProblemWith(entry.Metadata, property.Metadata, property.CurrentValue))
            .OfType<StoredValueProblem>();

    private static StoredValueProblem? ProblemWith(IEntityType record, IProperty column, object? value) => value switch
    {
        null => MissingValueProblem(record, column),
        string text => TextProblem(record, column, text),
        decimal number => NumberProblem(record, column, number),
        _ => null
    };

    private static StoredValueProblem? MissingValueProblem(IEntityType record, IProperty column)
    {
        var isRequiredText = column.ClrType == typeof(string) && !column.IsNullable;
        if (!isRequiredText) return null;
        return Problem(record, column, field => $"{field} is required.");
    }

    private static StoredValueProblem? TextProblem(IEntityType record, IProperty column, string text)
    {
        var longest = column.GetMaxLength();
        var isUnbounded = longest is null or > LongestBoundedNvarchar;
        if (isUnbounded || text.Length <= longest) return null;
        return Problem(record, column, field =>
            $"{field} is {text.Length:N0} characters long; it can hold at most {longest:N0}. Shorten it and save again.");
    }

    private static StoredValueProblem? NumberProblem(IEntityType record, IProperty column, decimal number)
    {
        var precision = column.GetPrecision() ?? DefaultDecimalPrecision;
        var scale = column.GetScale() ?? DefaultDecimalScale;
        var wholeDigits = precision - scale;
        if (wholeDigits > WholeDigitsDecimalAlwaysHolds) return null;
        var ceiling = DecimalCeiling(wholeDigits);
        var size = Math.Abs(number);
        if (size < ceiling) return null;
        return Problem(record, column, field =>
            $"{field} is {number:N} — too large to store; it must be less than {ceiling:N0}.");
    }

    private static decimal DecimalCeiling(int wholeDigits) =>
        Enumerable.Repeat(10m, wholeDigits).Aggregate(1m, (product, ten) => product * ten);

    private static StoredValueProblem Problem(IEntityType record, IProperty column, Func<string, string> sentenceFor)
    {
        var recordName = StoredValueNames.Of(record);
        var fieldName = StoredValueNames.Of(column);
        return new StoredValueProblem(recordName, fieldName, sentenceFor($"{fieldName} on the {recordName.ToLowerInvariant()}"));
    }
}
