using Jewel.JPMS.Api.Features.Ai.Sources;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiSourceTools
{
    private sealed record Opened(AiSourceDocument? Document, string? FileName, string? Failure);

    private static async Task<Opened> OpenAsync(AiToolContext context, string sourceId, CancellationToken ct)
    {
        if (sourceId.StartsWith(MailPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var rest = sourceId[MailPrefix.Length..];
            var split = rest.LastIndexOf(MailSeparator);
            if (split <= 0 || split == rest.Length - 1)
                return new Opened(null, null, $"\"{sourceId}\" is not a mail source id — they look like mail:<messageId>|<attachmentId>, as list_sources returns them.");
            return await OpenMailAsync(context, rest[..split], rest[(split + 1)..], ct);
        }
        if (AiFiledDocuments.IsFiledHandle(sourceId))
        {
            // A document filed in the portal — the contract, an Architect's Instruction, a
            // drawing, a certificate, a Document Control item, a compliance file — gated exactly
            // as its download endpoint is.
            var filed = await AiFiledDocuments.OpenAsync(context, sourceId, ct);
            if (filed.Failure is not null) return new Opened(null, filed.FileName, filed.Failure);
            return Load(filed.FileName!, filed.ContentType, filed.Bytes!);
        }
        return new Opened(null, null, $"\"{sourceId}\" is not a source id. list_sources returns them: mail:… for an "
            + "email attachment, contract:/amendment:/ai:/drawing:/cert:/doc:/compliance:… "
            + "for a document filed in the portal.");
    }

    private static async Task<Opened> OpenMailAsync(AiToolContext context, string messageId, string attachmentId, CancellationToken ct)
    {
        IntakeAttachmentContent? file;
        try
        {
            var reader = context.Services.GetRequiredService<IIntakeMessageReader>();
            file = await reader.GetAttachmentAsync(messageId, attachmentId, ct);
        }
        catch (Exception ex)
        {
            return new Opened(null, null, $"The attachment could not be fetched from the mailbox ({ex.Message}).");
        }
        if (file is null)
            return new Opened(null, null, "That attachment could not be fetched — it may be an attached email or a link rather than a file.");

        if (file.Content.Length > MaxSourceBytes)
        {
            return new Opened(null, null, $"\"{file.Name}\" is {file.Content.Length / 1_048_576.0:0.#} MB — too big to read "
                + "here. Tell the user which file holds the answer and ask them to open it themselves.");
        }

        return Load(file.Name, file.ContentType, file.Content);
    }

    private static Opened Load(string fileName, string? contentType, byte[] bytes)
    {
        try
        {
            return new Opened(AiSourceReader.Load(fileName, contentType, bytes), fileName, null);
        }
        catch (Exception ex) when (ex is InvalidDataException or NotSupportedException)
        {
            // The reader's sentences are written to be relayed (scan with no text layer,
            // password-protected, legacy format) — pass them through.
            return new Opened(null, fileName, $"\"{fileName}\" could not be read: {ex.Message} Tell the user the "
                + "answer appears to be in this file and ask them what it says.");
        }
    }
}
