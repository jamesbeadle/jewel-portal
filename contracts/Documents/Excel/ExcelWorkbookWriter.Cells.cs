using System.Globalization;
using System.Text;

namespace Jewel.JPMS.Contracts.Documents.Excel;

public static partial class ExcelWorkbookWriter
{
    private static void AppendCell(StringBuilder builder, string cellRef, ExcelFormat columnFormat, object? value, ExcelStyleRegistry styles)
    {
        if (value is ExcelStyledCell styled)
        {
            var styleId = styles.For(styled.Style);
            // A styled null still renders — that's how band fills and spacer cells exist.
            if (styled.Value is null)
            {
                builder.Append($"""<c r="{cellRef}" s="{styleId}"/>""");
                return;
            }
            AppendValue(builder, cellRef, styleId, styled.Value, styles, styled.Style.Format);
            return;
        }

        if (value is null)
        {
            return;
        }

        AppendValue(builder, cellRef, styles.For(columnFormat), value, styles, columnFormat);
    }

    private static void AppendValue(StringBuilder builder, string cellRef, int styleId, object value, ExcelStyleRegistry styles, ExcelFormat format)
    {
        switch (value)
        {
            case DateTimeOffset dto:
                AppendNumber(builder, cellRef, DateStyle(styleId, format, styles), dto.DateTime.ToOADate());
                break;
            case DateTime dt:
                AppendNumber(builder, cellRef, DateStyle(styleId, format, styles), dt.ToOADate());
                break;
            case DateOnly d:
                AppendNumber(builder, cellRef, DateStyle(styleId, format, styles), d.ToDateTime(TimeOnly.MinValue).ToOADate());
                break;
            case bool b:
                AppendInlineString(builder, cellRef, styleId, b ? "Yes" : "No");
                break;
            case decimal or double or float or int or long or short or byte or uint or ulong or ushort or sbyte:
                AppendNumber(builder, cellRef, styleId, Convert.ToDouble(value, CultureInfo.InvariantCulture));
                break;
            default:
                AppendInlineString(builder, cellRef, styleId, value.ToString() ?? "");
                break;
        }
    }

    /// <summary>Dates always take a date style so a mistyped column format still yields a readable cell.</summary>
    private static int DateStyle(int styleId, ExcelFormat format, ExcelStyleRegistry styles) =>
        format is ExcelFormat.Date or ExcelFormat.DateTime
            ? styleId
            : styles.For(ExcelFormat.Date);

    private static void AppendNumber(StringBuilder builder, string cellRef, int style, double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return;
        }
        builder.Append($"""<c r="{cellRef}" s="{style}"><v>{value.ToString("R", CultureInfo.InvariantCulture)}</v></c>""");
    }

    private static void AppendInlineString(StringBuilder builder, string cellRef, int style, string value)
    {
        if (value.Length == 0)
        {
            builder.Append($"""<c r="{cellRef}" s="{style}"/>""");
            return;
        }
        // preserve leading/trailing whitespace per the OOXML spec
        var space = value[0] == ' ' || value[^1] == ' ' ? " xml:space=\"preserve\"" : "";
        builder.Append($"""<c r="{cellRef}" s="{style}" t="inlineStr"><is><t{space}>{Escape(value)}</t></is></c>""");
    }
}
