namespace Jewel.JPMS.Models;

/// <summary>What one cell of a repeating row asks for. Label is a cell the definition fills in — the item
/// on a checklist — and the person reads, never types.</summary>
public enum FormColumnKind
{
    Text = 0,
    Date = 1,
    Choice = 2,
    Tick = 3,
    Label = 4
}

/// <summary>One column of a repeating row on a paper register: its key, the heading the person reads and what it takes.</summary>
public sealed record FormColumn(string Key, string Label, FormColumnKind Kind, IReadOnlyList<string>? Choices = null)
{
    public IReadOnlyList<string> Choices { get; init; } = Choices ?? Array.Empty<string>();

    public bool IsTyped => Kind != FormColumnKind.Label;
}

/// <summary>
/// The rows a register question takes — a paper form's ruled table: the attendance on a toolbox
/// talk, the extinguishers on a fire point sheet, the items on a first-aid kit checklist. A table
/// with FixedRows is a checklist: the rows are the definition's, in its order, and the person fills
/// in the typed cells; without them the person adds one row per RowNoun up to MostRows.
/// </summary>
public sealed record FormTable(
    IReadOnlyList<FormColumn> Columns,
    string RowNoun,
    IReadOnlyList<IReadOnlyDictionary<string, string>>? FixedRows = null,
    int MostRows = FormTable.MostRowsAllowed)
{
    public const int MostRowsAllowed = 60;

    public IReadOnlyList<IReadOnlyDictionary<string, string>> FixedRows { get; init; } =
        FixedRows ?? Array.Empty<IReadOnlyDictionary<string, string>>();

    public bool HasFixedRows => FixedRows.Count > 0;

    public IEnumerable<FormColumn> TypedColumns => Columns.Where(column => column.IsTyped);

    public FormColumn? ColumnFor(string key) => Columns.FirstOrDefault(column => column.Key == key);
}

/// <summary>The words a table's columns are written in, read like the questions above them.</summary>
public static class Column
{
    public static FormColumn Text(string key, string label) => new(key, label, FormColumnKind.Text);

    public static FormColumn Date(string key, string label) => new(key, label, FormColumnKind.Date);

    public static FormColumn Choice(string key, string label, string[] choices) => new(key, label, FormColumnKind.Choice, choices);

    public static FormColumn Tick(string key, string label) => new(key, label, FormColumnKind.Tick);

    public static FormColumn Label(string key, string label) => new(key, label, FormColumnKind.Label);
}
