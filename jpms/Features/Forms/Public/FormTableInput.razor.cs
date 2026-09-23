using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Features.Forms.Public;

/// <summary>
/// The rows of a table question, edited in place and written back to the draft as the one JSON
/// string the engine stores (FormTableAnswers). The draft is the single copy: every change re-reads
/// the rows from it, so a resumed draft shows exactly what was typed before the reload.
/// </summary>
public partial class FormTableInput
{
    [Parameter, EditorRequired] public FormDefinition Form { get; set; } = default!;
    [Parameter, EditorRequired] public FormQuestion Question { get; set; } = default!;
    [Parameter, EditorRequired] public FormDraft Draft { get; set; } = default!;
    [Parameter] public EventCallback OnChanged { get; set; }

    private FormTable Table => Question.Table!;

    private IReadOnlyList<IReadOnlyDictionary<string, string>> Rows => RowsOnTheDraft();

    protected override void OnInitialized()
    {
        var isBlank = FormTableAnswers.Read(Draft.AnswerTo(Question.Key)).Count == 0;
        if (isBlank) WriteRows(StartingRows());
    }

    private IReadOnlyList<IReadOnlyDictionary<string, string>> RowsOnTheDraft()
    {
        var kept = FormTableAnswers.Read(Draft.AnswerTo(Question.Key));
        return kept.Count == 0 ? StartingRows() : kept;
    }

    private IReadOnlyList<IReadOnlyDictionary<string, string>> StartingRows() =>
        Table.HasFixedRows ? Table.FixedRows : new[] { (IReadOnlyDictionary<string, string>)new Dictionary<string, string>() };

    private string RowTitle(IReadOnlyDictionary<string, string> row, int index)
    {
        var label = Table.Columns.FirstOrDefault(column => !column.IsTyped);
        if (label is not null) return row.GetValueOrDefault(label.Key, "");
        return $"{Capitalised(Table.RowNoun)} {index + 1}";
    }

    private static string CellOf(IReadOnlyDictionary<string, string> row, FormColumn column) => row.GetValueOrDefault(column.Key, "");

    private string CellId(int index, FormColumn column) => $"question-{Question.Key}-{index}-{column.Key}";

    private async Task CellChangedAsync(int index, FormColumn column, string value)
    {
        var rows = Rows.Select(row => new Dictionary<string, string>(row)).ToList();
        rows[index][column.Key] = value;
        WriteRows(rows);
        await OnChanged.InvokeAsync();
    }

    private async Task AddRowAsync()
    {
        var rows = Rows.ToList();
        rows.Add(new Dictionary<string, string>());
        WriteRows(rows);
        await OnChanged.InvokeAsync();
    }

    private async Task RemoveAsync(int index)
    {
        var rows = Rows.ToList();
        rows.RemoveAt(index);
        WriteRows(rows);
        await OnChanged.InvokeAsync();
    }

    private void WriteRows(IReadOnlyList<IReadOnlyDictionary<string, string>> rows) =>
        Draft.Answer(Form, Question.Key, FormTableAnswers.Write(rows));

    private static string Capitalised(string noun) => noun.Length == 0 ? noun : char.ToUpperInvariant(noun[0]) + noun[1..];
}
