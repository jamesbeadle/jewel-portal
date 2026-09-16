using Jewel.JPMS.Api.Features.Ai.Sources;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiSourceTools
{
    /// <summary>A source's bytes as they are, for a caller that wants the FILE rather than a
    /// reading of it — the photo intake. Same ids list_sources hands out, same gates OpenAsync
    /// applies; a failure names the reason.</summary>
    internal sealed record SourceBytes(byte[]? Bytes, string? FileName, string? ContentType, string? Failure);

    internal static async Task<SourceBytes> FetchBytesAsync(AiToolContext context, string sourceId, CancellationToken ct)
    {
        if (sourceId.StartsWith(MailPrefix, StringComparison.OrdinalIgnoreCase))
            return await FetchMailBytesAsync(context, sourceId, ct);

        if (AiFiledDocuments.IsFiledHandle(sourceId))
        {
            var filed = await AiFiledDocuments.OpenAsync(context, sourceId, ct);
            return new SourceBytes(filed.Bytes, filed.FileName, filed.ContentType, filed.Failure);
        }

        return new SourceBytes(null, null, null, $"\"{sourceId}\" is not a source id. list_sources returns them: mail:… for an "
            + "email attachment, doc:/drawing:/… for a document filed in the portal.");
    }

    private static async Task<SourceBytes> FetchMailBytesAsync(AiToolContext context, string sourceId, CancellationToken ct)
    {
        var rest = sourceId[MailPrefix.Length..];
        var split = rest.LastIndexOf(MailSeparator);
        if (split <= 0 || split == rest.Length - 1)
            return new SourceBytes(null, null, null, $"\"{sourceId}\" is not a mail source id — they look like mail:<messageId>|<attachmentId>, as list_sources returns them.");

        IntakeAttachmentContent? file;
        try
        {
            var reader = context.Services.GetRequiredService<IIntakeMessageReader>();
            file = await reader.GetAttachmentAsync(rest[..split], rest[(split + 1)..], ct);
        }
        catch (Exception ex)
        {
            return new SourceBytes(null, null, null, $"The attachment could not be fetched from the mailbox ({ex.Message}).");
        }
        if (file is null)
            return new SourceBytes(null, null, null, "That attachment could not be fetched — it may be an attached email or a link rather than a file.");
        return new SourceBytes(file.Content, file.Name, file.ContentType, null);
    }
}
