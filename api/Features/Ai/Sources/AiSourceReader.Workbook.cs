using ClosedXML.Excel;

namespace Jewel.JPMS.Api.Features.Ai.Sources;

internal static partial class AiSourceReader
{
    private static AiSourceDocument LoadWorkbook(byte[] content)
    {
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(new MemoryStream(content));
        }
        catch (Exception ex)
        {
            throw new InvalidDataException(
                "That file could not be opened as a spreadsheet — if it is an old .xls, save it as .xlsx first.", ex);
        }

        using (workbook)
        {
            var parts = new List<AiSourcePart>();
            foreach (var sheet in workbook.Worksheets)
            {
                // Contents only — values and formulas. The default (AllContents) counts data
                // validation, conditional formats and merged ranges, so a whole-column dropdown
                // (D:D) reports the last row as 1,048,576 and the loop below would build a
                // million empty rows at upload and on every read.
                var lastRow = sheet.LastRowUsed(XLCellsUsedOptions.Contents);
                var lastColumn = sheet.LastColumnUsed(XLCellsUsedOptions.Contents);
                if (lastRow is null || lastColumn is null) continue;

                var lastRowNumber = lastRow.RowNumber();
                var lastColumnNumber = lastColumn.ColumnNumber();
                var rows = new List<string>(lastRowNumber);
                for (var rowNumber = 1; rowNumber <= lastRowNumber; rowNumber++)
                {
                    var row = sheet.Row(rowNumber);
                    var cells = new string[lastColumnNumber];
                    for (var columnNumber = 1; columnNumber <= lastColumnNumber; columnNumber++)
                    {
                        // GetFormattedString so dates read as dates and money as money — the
                        // DISPLAYED value is what the boss meant, not the raw serial behind it.
                        // Every column from 1 to the last used one, blanks included, so the
                        // columns stay aligned across rows (the old CellsUsed join collapsed
                        // them and a price could slide under the wrong heading).
                        cells[columnNumber - 1] = row.Cell(columnNumber).GetFormattedString().Trim();
                    }
                    rows.Add(string.Join('\t', cells).TrimEnd('\t'));
                }
                parts.Add(new AiSourcePart(sheet.Name, sheet.Name, "row", rows));
            }

            if (parts.Count == 0)
                throw new InvalidDataException("That spreadsheet has no readable content — every sheet is empty.");

            return new AiSourceDocument(AiSourceDocument.Workbook, parts);
        }
    }
}
