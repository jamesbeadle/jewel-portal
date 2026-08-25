using System.Text.Json;
using Jewel.JPMS.Api.Features.TenderEnquiries;
using Jewel.JPMS.Api.Features.TenderEnquiries.Attachments;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The assistant's view of a tender enquiry (2026-08-25, James: "I'd use the AI chat to draft
/// everything"): the record in one call, and the documents kept on it — the PQQ as received
/// above all — opened as text so the questions can be lifted and answered. Same read set as the
/// enquiry pages (every internal role); same file reader as email attachments.
/// </summary>
public static class AiTenderEnquiryTools
{
    private const int DefaultMaxChars = 20_000;
    private const int MinMaxChars = 2_000;
    private const int MaxMaxChars = 50_000;

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    public static IReadOnlyList<AiTool> Build() => new List<AiTool>
    {
        new(
            "get_tender_enquiry_context",
            "Everything held ON a tender enquiry record, in one call: reference, title, the "
            + "architect and contact, scope of works, contract form, status and dates, the PQQ "
            + "answers as they stand, and the documents kept on it (the questionnaire as received, "
            + "the drawings) with the ids read_tender_enquiry_document opens them by. Call this "
            + "FIRST when drafting a PQQ response or answering questions about an enquiry; the "
            + "tagged emails are separate — read_record_emails (record_type tender_enquiry) has "
            + "those. Defaults to the enquiry on the page in view.",
            AiToolSchema.Object(
                ("tenderEnquiryId", "string",
                    "The enquiry's id. Defaults to the record in view when the user is on its page.", false)),
            AiToolKind.Read,
            JpmsRoleSets.AllInternal,
            GetContextAsync),

        new(
            "read_tender_enquiry_document",
            "One document kept on a tender enquiry, by the documentId get_tender_enquiry_context "
            + "returned — the PQQ PDF as the architect sent it, a drawing, supporting material. "
            + "PDFs and Word documents come back as text, spreadsheets as tab-separated rows, "
            + "images are shown to you. What cannot be read is refused with the reason — relay "
            + "it rather than guessing.",
            AiToolSchema.Object(
                ("documentId", "string", "The document's id from get_tender_enquiry_context.", true),
                ("maxChars", "number",
                    "How much extracted text to return. Default 20000, minimum 2000, maximum 50000.", false)),
            AiToolKind.Read,
            JpmsRoleSets.AllInternal,
            ReadDocumentAsync)
    };

    private static async Task<string> GetContextAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var tenderEnquiryId = AiToolSchema.Text(input, "tenderEnquiryId") ?? EnquiryIdInView(context);
        if (string.IsNullOrWhiteSpace(tenderEnquiryId))
            return Fail("Say which tender enquiry: pass tenderEnquiryId, or have the user open its page.");

        var entity = await context.Db.TenderEnquiries.AsNoTracking()
            .FirstOrDefaultAsync(row => row.TenderEnquiryId == tenderEnquiryId, ct);
        if (entity is null) return Fail($"No tender enquiry found with id {tenderEnquiryId}.");
        var enquiry = entity.ToModel();

        var answers = await TenderEnquiryAnswerReader.ListAsync(context.Db, tenderEnquiryId!, ct);
        var documents = await context.Db.TenderEnquiryAttachments.AsNoTracking()
            .Where(row => row.TenderEnquiryId == tenderEnquiryId)
            .OrderBy(row => row.AddedAt)
            .Select(row => new { documentId = row.TenderEnquiryAttachmentId, row.FileName, row.ContentType, row.FileSizeBytes })
            .ToListAsync(ct);

        return Serialise(new
        {
            ok = true,
            reference = enquiry.Reference,
            enquiry.Title,
            architect = new { practice = enquiry.ArchitectPracticeName, contact = enquiry.ArchitectContactName, email = enquiry.ArchitectContactEmail },
            enquiry.ScopeSummary,
            enquiry.ContractForm,
            status = enquiry.Status.DisplayName(),
            receivedAt = enquiry.ReceivedAt,
            pqqDueAt = enquiry.PqqDueAt,
            tenderDueAt = enquiry.TenderDueAt,
            answers = answers.Select(answer => new { answer.Position, answer.Question, answer.Answer }),
            documents,
            note = "Open a document with read_tender_enquiry_document. Tagged emails are separate — "
                   + "read_record_emails (record_type tender_enquiry) returns them with full bodies and attachment ids."
        });
    }

    private static async Task<string> ReadDocumentAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var documentId = AiToolSchema.Text(input, "documentId");
        if (string.IsNullOrWhiteSpace(documentId)) return Fail("A documentId is required — get_tender_enquiry_context returns them.");

        var row = await context.Db.TenderEnquiryAttachments.AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.TenderEnquiryAttachmentId == documentId, ct);
        if (row is null || string.IsNullOrWhiteSpace(row.BlobRef)) return Fail($"No document found with id {documentId}.");

        var store = context.Services.GetRequiredService<ITenderEnquiryAttachmentStore>();
        var blob = await store.OpenAsync(row.BlobRef, ct);
        if (blob is null) return Fail($"\"{row.FileName}\" is registered but its file could not be found in storage.");

        byte[] content;
        await using (blob.Content)
        {
            using var buffer = new MemoryStream();
            await blob.Content.CopyToAsync(buffer, ct);
            content = buffer.ToArray();
        }

        var limit = (int)Math.Clamp(AiToolSchema.Number(input, "maxChars") ?? DefaultMaxChars, MinMaxChars, MaxMaxChars);
        return AiRecordTools.ReadFileForModel(row.FileName, row.ContentType, content, limit);
    }

    private static string? EnquiryIdInView(AiToolContext context) =>
        AiRecordTools.TryMapRecordType(context.Scope?.RecordType ?? "", out var scopeType) && scopeType == RecordType.TenderEnquiry
            ? context.Scope?.RecordId
            : null;
}
