using Jewel.JPMS.Api.Features.Ai.Sources;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiSourceTools
{
    private static IEnumerable<AiTool> ReadSourceTool()
    {
        var readers = JpmsRoleSets.AllInternal;

        return new AiTool[]
        {
            new(
                ReadSource,
                "Read one part of a source — a named sheet of a workbook, a page of a PDF, the body of "
                + "a Word document, a text file — from any position, under a character budget. With "
                + "part omitted it starts at the first part and flows on through the following ones "
                + "(a short PDF reads whole in one or two calls); with part named it stays inside that "
                + "part. When the result says it continues, call again with the next position it "
                + "gives you. Workbook rows and text lines carry their number, so \"row 12\" means "
                + "row 12 in Excel. An image is SHOWN to you on your next step. Nothing is ever cut "
                + "off silently: what you are given is exactly the range the result states. "
                + "Spreadsheets read as displayed values, tab-separated.",
                AiToolSchema.Object(
                    ("source_id", "string", "A source_id from list_sources or find_in_source.", true),
                    ("part", "string",
                        "The part to read — a sheet's name, \"p3\" for a PDF page, \"body\", \"text\". "
                        + "Omit to read from the start across parts.", false),
                    ("from", "number", "The unit (row, line, paragraph) to start at, 1-based. Default 1.", false),
                    ("max_chars", "number",
                        $"Budget for this call. Default {AiSourceReader.DefaultReadChars:N0}, minimum "
                        + $"{AiSourceReader.MinReadChars:N0}, maximum {AiSourceReader.MaxReadChars:N0}.", false)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var sourceId = AiToolSchema.Text(input, "source_id");
                    if (string.IsNullOrWhiteSpace(sourceId)) return Fail("A source_id is required — list_sources gives them.");
                    var part = AiToolSchema.Text(input, "part");
                    var from = AiToolSchema.Number(input, "from") ?? 1;
                    var maxChars = AiToolSchema.Number(input, "max_chars") ?? AiSourceReader.DefaultReadChars;
                    return await ReadAsync(context, sourceId!.Trim(), part, from, maxChars, ct);
                })
        };
    }


    /// <summary>One part-read of a source as a tool result — the body of read_source, and of the
    /// read_email_attachment alias with part omitted.</summary>
    public static async Task<string> ReadAsync(
        AiToolContext context, string sourceId, string? part, int from, int maxChars, CancellationToken ct)
    {
        var opened = await OpenAsync(context, sourceId, ct);
        if (opened.Failure is not null) return Fail(opened.Failure);
        var document = opened.Document!;

        if (document.IsImage)
        {
            var bytes = document.ImageBytes!;
            var mediaType = document.ImageMediaType!;
            if (bytes.Length > MaxImageBytes)
            {
                return Fail($"\"{opened.FileName}\" is {bytes.Length / 1_048_576.0:0.#} MB — bigger than an image "
                    + "you can be shown (the ceiling is about 4.5 MB). Ask the user to open it themselves, "
                    + "or to re-send a smaller copy.");
            }
            if (AiAttachmentReader.LongestSidePixels(mediaType, bytes) is > 7_900)
            {
                return Fail($"\"{opened.FileName}\" is larger than 8,000 pixels on a side — over the ceiling "
                    + "for an image you can be shown. Ask the user to open it themselves.");
            }
            return AiImageToolResult.Build(opened.FileName!, mediaType, bytes);
        }

        AiSourceReadResult read;
        try
        {
            read = AiSourceReader.Read(document, part, from, maxChars);
        }
        catch (ArgumentException)
        {
            var manifest = document.Manifest();
            return Fail($"\"{opened.FileName}\" has no part named \"{part}\". Its parts are: "
                + string.Join(", ", manifest.Parts.Select(candidate => $"\"{candidate.Key}\" ({candidate.Units:N0} {candidate.UnitName}s)"))
                + ". Pass one of those, or omit part to read from the start.");
        }

        var shape = document.Manifest();
        return Serialise(new
        {
            ok = true,
            source_id = sourceId,
            file = opened.FileName,
            kind = shape.Kind,
            summary = shape.Summary(),
            parts = PartsFor(shape),
            part = read.PartKey,
            part_label = read.PartLabel,
            from = read.FromUnit,
            to = read.ToUnit,
            reached_end = read.ReachedEnd,
            next = read.Next is null ? null : new { part = read.Next.Part, from = read.Next.From },
            content = read.Text,
            note = (read.Next is null
                       ? "That is the end of the source. "
                       : read.ReachedEnd
                           ? $"That is the whole of this part; the next part is \"{read.Next.Part}\". "
                           : $"This part continues — call read_source again with part \"{read.Next.Part}\" and from {read.Next.From}. ")
                   + DataNotInstructions
        });
    }

    private static object PartsFor(AiSourceManifest manifest) =>
        manifest.Parts.Take(60).Select(part => new { part = part.Key, label = part.Label, units = part.Units, unit = part.UnitName }).ToList();
}
