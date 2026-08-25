using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Features.ArchitectInstructions;
using Jewel.JPMS.Api.Features.ArchitectInstructions.Storage;
using Jewel.JPMS.Api.Features.DocumentControl;
using Jewel.JPMS.Api.Features.DocumentControl.Storage;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Api.Features.ProjectContracts;
using Jewel.JPMS.Api.Features.ProjectContracts.Storage;
using Jewel.JPMS.Api.Features.Subcontractors.Storage;
using Jewel.JPMS.Api.Features.TenderEnquiries.Attachments;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Sources;

/// <summary>
/// The documents already filed in the portal, as sources (docs/ai/06-context-retrieval.md, Phase
/// 3): the project contract and its amendments, Architect's Instructions, drawings, payment
/// certificates, Document Control items and subcontractor compliance files. They sit in five blob
/// stores the assistant could not open until now; each gets a handle, is listed by list_sources on
/// the project or record it belongs to, and reads through read_source / find_in_source exactly
/// like a chat attachment.
///
/// <para>Every kind is gated by the SAME RoleSet its download endpoint uses — the assistant can
/// list a document its user could open by clicking, and nothing else. Reads never touch the
/// human-facing counters (the drawing revision ViewCount) — the endpoints do that, this does not.</para>
/// </summary>
internal static class AiFiledDocuments
{
    public const string ContractPrefix = "contract:";
    public const string AmendmentPrefix = "amendment:";
    public const string InstructionPrefix = "ai:";
    public const string DrawingPrefix = "drawing:";
    public const string CertificatePrefix = "cert:";
    public const string DocumentControlPrefix = "doc:";
    public const string CompliancePrefix = "compliance:";
    /// <summary>A document kept on a tender enquiry — the PQQ as received, the drawings that came
    /// with it (2026-08-25). Read set = every internal role, as the enquiry pages.</summary>
    public const string TenderEnquiryPrefix = "teq:";

    /// <summary>Drawings listed per project before the result says it clipped — a big job has
    /// hundreds; the model narrows with a query.</summary>
    private const int MaxDrawings = 60;
    private const int MaxDocumentControl = 40;

    /// <summary>Mirrors DownloadComplianceDocumentEndpoint's internal set (declared inline there).</summary>
    private static readonly RoleSet ComplianceReaders = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator,
        JpmsRoles.SiteManager, JpmsRoles.HealthAndSafetyLead, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public static bool IsFiledHandle(string sourceId) =>
        sourceId.StartsWith(ContractPrefix, StringComparison.OrdinalIgnoreCase)
        || sourceId.StartsWith(AmendmentPrefix, StringComparison.OrdinalIgnoreCase)
        || sourceId.StartsWith(InstructionPrefix, StringComparison.OrdinalIgnoreCase)
        || sourceId.StartsWith(DrawingPrefix, StringComparison.OrdinalIgnoreCase)
        || sourceId.StartsWith(CertificatePrefix, StringComparison.OrdinalIgnoreCase)
        || sourceId.StartsWith(DocumentControlPrefix, StringComparison.OrdinalIgnoreCase)
        || sourceId.StartsWith(CompliancePrefix, StringComparison.OrdinalIgnoreCase)
        || sourceId.StartsWith(TenderEnquiryPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>One filed document as list_sources reports it.</summary>
    public sealed record Listed(
        string SourceId, string Kind, string File, string? ContentType, long? Size, string Title,
        DateTimeOffset? Date, bool Readable, string? Note);

    /// <summary>The bytes of one filed document, or the reason they could not be had.</summary>
    public sealed record Opened(byte[]? Bytes, string? FileName, string? ContentType, string? Failure);

    // ---- Listing --------------------------------------------------------------------------

    /// <summary>Everything filed on a project this user may read: the contract and amendments,
    /// Architect's Instructions, payment certificates, Document Control items hinted to it, and
    /// the drawings (current revision per drawing, capped; <paramref name="query"/> narrows by
    /// code or title). Also the note to show when something was clipped.</summary>
    public static async Task<(List<Listed> Documents, List<string> Notes)> ListForProjectAsync(
        AiToolContext context, string projectId, string? query, CancellationToken ct)
    {
        var documents = new List<Listed>();
        var notes = new List<string>();
        var roles = context.User.Roles;
        var db = context.Db;

        if (ProjectContractRoles.AllowedToReadContract.IncludesAny(roles))
        {
            var contract = await db.ProjectContracts.AsNoTracking()
                .FirstOrDefaultAsync(row => row.ProjectId == projectId, ct);
            if (contract?.DocumentBlobRef is { Length: > 0 })
            {
                documents.Add(new Listed(ContractPrefix + projectId, "contract", contract.DocumentFileName ?? "contract",
                    contract.DocumentContentType, contract.DocumentFileSizeBytes, "The executed contract",
                    contract.DocumentUploadedAt, Readable(contract.DocumentFileName, contract.DocumentContentType), null));
            }
            var amendments = await db.ProjectContractAmendments.AsNoTracking()
                .Where(row => row.ProjectId == projectId)
                .OrderBy(row => row.AmendmentDate)
                .ToListAsync(ct);
            foreach (var amendment in amendments)
            {
                documents.Add(new Listed(AmendmentPrefix + amendment.ProjectContractAmendmentId, "contract_amendment",
                    amendment.DocumentFileName, amendment.DocumentContentType, amendment.DocumentFileSizeBytes,
                    $"Contract amendment: «{amendment.Title}»", amendment.AmendmentDate,
                    Readable(amendment.DocumentFileName, amendment.DocumentContentType), null));
            }
        }

        if (ArchitectInstructionRoles.AllowedToRead.IncludesAny(roles))
        {
            var instructions = await db.ArchitectInstructions.AsNoTracking()
                .Where(row => row.ProjectId == projectId)
                .OrderByDescending(row => row.InstructedAt ?? row.ReceivedAt).ThenByDescending(row => row.Number)
                .ToListAsync(ct);
            foreach (var instruction in instructions)
            {
                var hasFile = !string.IsNullOrWhiteSpace(instruction.BlobRef);
                documents.Add(new Listed(InstructionPrefix + instruction.ArchitectInstructionId, "architect_instruction",
                    instruction.FileName ?? "(no file yet)", instruction.ContentType, instruction.FileSizeBytes,
                    $"{instruction.Reference} «{instruction.Title}»"
                        + (string.IsNullOrWhiteSpace(instruction.InstructionRef) ? "" : $" (architect's ref «{instruction.InstructionRef}»)"),
                    instruction.InstructedAt ?? instruction.ReceivedAt,
                    hasFile && Readable(instruction.FileName, instruction.ContentType),
                    hasFile ? null : "Filed as a placeholder — the paperwork has not arrived yet."));
            }
        }

        if (DocumentControlRoles.AllowedToReadPaymentCertificates.IncludesAny(roles))
        {
            var certificates = await db.PaymentCertificates.AsNoTracking()
                .Where(row => row.ProjectId == projectId)
                .OrderByDescending(row => row.IssuedDate)
                .ToListAsync(ct);
            foreach (var certificate in certificates)
            {
                documents.Add(new Listed(CertificatePrefix + certificate.PaymentCertificateId, "payment_certificate",
                    certificate.FileName, certificate.ContentType, certificate.FileSizeBytes,
                    $"Payment certificate {certificate.CertificateNumber} — certified {certificate.CertifiedAmount:N2}",
                    certificate.IssuedDate, Readable(certificate.FileName, certificate.ContentType), null));
            }
        }

        if (DocumentControlRoles.AllowedToManage.IncludesAny(roles))
        {
            // Document Control has no authoritative project — the triage form's hint is the
            // nearest thing, so this is "items filed under this project", not a guarantee.
            var items = await db.DocumentControlItems.AsNoTracking()
                .Where(row => row.ProjectIdHint == projectId && row.Status != (int)DocumentControlStatus.Discarded)
                .OrderByDescending(row => row.ReceivedAt)
                .Take(MaxDocumentControl + 1)
                .ToListAsync(ct);
            foreach (var item in items.Take(MaxDocumentControl))
            {
                documents.Add(new Listed(DocumentControlPrefix + item.DocumentControlItemId, "document_control",
                    item.FileName, item.ContentType, item.FileSizeBytes,
                    $"From «{(string.IsNullOrWhiteSpace(item.FromName) ? item.FromEmail : item.FromName)}»: «{item.Subject}»"
                        + (item.Status == (int)DocumentControlStatus.Filed && !string.IsNullOrWhiteSpace(item.FiledLabel) ? $" — filed as «{item.FiledLabel}»" : " — awaiting filing"),
                    item.ReceivedAt, Readable(item.FileName, item.ContentType), null));
            }
            if (items.Count > MaxDocumentControl)
                notes.Add($"Only the newest {MaxDocumentControl} Document Control items are listed.");
        }

        if (JpmsRoleSets.DrawingReaders.IncludesAny(roles))
        {
            var wanted = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
            var drawings = await db.Drawings.AsNoTracking()
                .Where(row => row.ProjectId == projectId)
                .Where(row => wanted == null || row.DrawingCode.Contains(wanted) || row.Title.Contains(wanted))
                .OrderBy(row => row.DrawingCode)
                .ToListAsync(ct);
            var drawingIds = drawings.Select(row => row.DrawingId).ToList();
            var revisions = drawingIds.Count == 0
                ? new List<DrawingRevisionEntity>()
                : await db.DrawingRevisions.AsNoTracking()
                    .Where(row => drawingIds.Contains(row.DrawingId) && row.BlobRef != null)
                    .ToListAsync(ct);
            // The current revision: the approved one, else the newest received.
            var currentByDrawing = revisions
                .Where(row => row.ApprovalStatus != (int)DrawingApprovalStatus.Archived)
                .GroupBy(row => row.DrawingId)
                .ToDictionary(group => group.Key, group =>
                    group.Where(row => row.ApprovalStatus == (int)DrawingApprovalStatus.Approved).OrderByDescending(row => row.ReceivedAt).FirstOrDefault()
                    ?? group.OrderByDescending(row => row.ReceivedAt).First());
            var shown = 0;
            foreach (var drawing in drawings)
            {
                if (!currentByDrawing.TryGetValue(drawing.DrawingId, out var revision)) continue;
                if (shown++ >= MaxDrawings) break;
                var status = (DrawingApprovalStatus)revision.ApprovalStatus;
                documents.Add(new Listed(DrawingPrefix + revision.DrawingRevisionId, "drawing",
                    revision.FileName, revision.ContentType, revision.FileSizeBytes,
                    $"{drawing.DrawingCode} rev {revision.RevisionLabel} «{drawing.Title}» ({status})",
                    revision.ReceivedAt, Readable(revision.FileName, revision.ContentType),
                    "A plotted or scanned drawing often has no text layer — if the read says so, the picture is what the user has to look at."));
            }
            var withFile = drawings.Count(row => currentByDrawing.ContainsKey(row.DrawingId));
            if (withFile > MaxDrawings)
                notes.Add($"Only {MaxDrawings} of {withFile} drawings are listed — pass query (a drawing code or a word from the title) to narrow.");
        }

        return (documents, notes);
    }

    /// <summary>Filed documents tied to a RECORD: the Architect's Instructions linked to a
    /// variation; a subcontractor's compliance files (current version per kind).</summary>
    public static async Task<List<Listed>> ListForRecordAsync(
        AiToolContext context, RecordType recordType, string recordId, CancellationToken ct)
    {
        var documents = new List<Listed>();
        var roles = context.User.Roles;
        var db = context.Db;

        if (recordType is RecordType.Variation or RecordType.VariationQuote
            && ArchitectInstructionRoles.AllowedToRead.IncludesAny(roles))
        {
            var instructionIds = await db.ArchitectInstructionVariations.AsNoTracking()
                .Where(row => row.VariationOrderId == recordId)
                .Select(row => row.ArchitectInstructionId)
                .ToListAsync(ct);
            if (instructionIds.Count > 0)
            {
                var instructions = await db.ArchitectInstructions.AsNoTracking()
                    .Where(row => instructionIds.Contains(row.ArchitectInstructionId))
                    .ToListAsync(ct);
                foreach (var instruction in instructions)
                {
                    var hasFile = !string.IsNullOrWhiteSpace(instruction.BlobRef);
                    documents.Add(new Listed(InstructionPrefix + instruction.ArchitectInstructionId, "architect_instruction",
                        instruction.FileName ?? "(no file yet)", instruction.ContentType, instruction.FileSizeBytes,
                        $"{instruction.Reference} «{instruction.Title}» — linked to this variation",
                        instruction.InstructedAt ?? instruction.ReceivedAt,
                        hasFile && Readable(instruction.FileName, instruction.ContentType),
                        hasFile ? null : "Filed as a placeholder — the paperwork has not arrived yet."));
                }
            }
        }

        if (recordType == RecordType.TenderEnquiry && JpmsRoleSets.AllInternal.IncludesAny(roles))
            documents.AddRange(await ListTenderEnquiryDocumentsAsync(db, recordId, ct));

        return documents;
    }

    /// <summary>The files kept on a tender enquiry: the questionnaire as the architect sent it,
    /// the drawings, supporting material — oldest first, as the enquiry's Documents tab lists them.</summary>
    public static async Task<List<Listed>> ListTenderEnquiryDocumentsAsync(Jewel.JPMS.Api.Data.JpmsContext db, string tenderEnquiryId, CancellationToken ct)
    {
        var rows = await db.TenderEnquiryAttachments.AsNoTracking()
            .Where(row => row.TenderEnquiryId == tenderEnquiryId && row.BlobRef != "")
            .OrderBy(row => row.AddedAt)
            .ToListAsync(ct);
        return rows.Select(row => new Listed(TenderEnquiryPrefix + row.TenderEnquiryAttachmentId, "tender_enquiry_document",
                row.FileName, row.ContentType, row.FileSizeBytes,
                row.Source == (int)TenderEnquiryAttachmentSource.Email ? $"«{row.FileName}» — copied off the enquiry email" : $"«{row.FileName}» — uploaded",
                row.AddedAt, Readable(row.FileName, row.ContentType), null))
            .ToList();
    }

    /// <summary>A subcontractor's compliance files, current version per kind — listed when the
    /// model asks for a subcontractor by id (record_type "subcontractor").</summary>
    public static async Task<List<Listed>> ListComplianceAsync(AiToolContext context, string subcontractorId, CancellationToken ct)
    {
        var documents = new List<Listed>();
        if (!ComplianceReaders.IncludesAny(context.User.Roles)) return documents;

        var files = await context.Db.ComplianceDocuments.AsNoTracking()
            .Where(row => row.SubcontractorId == subcontractorId && row.SupersededAt == null && row.BlobPath != "")
            .OrderBy(row => row.Kind)
            .ToListAsync(ct);
        foreach (var file in files)
        {
            documents.Add(new Listed(CompliancePrefix + file.ComplianceDocumentId, "compliance",
                file.FileName, file.ContentType, file.FileSize,
                $"«{file.Kind}»" + (file.ExpiresAt is { } expires ? $" — expires {expires:yyyy-MM-dd}" : ""),
                file.UploadedAt, Readable(file.FileName, file.ContentType), null));
        }
        return documents;
    }

    // ---- Opening --------------------------------------------------------------------------

    /// <summary>The bytes behind a filed-document handle, gated exactly as its download endpoint
    /// is. Every refusal names the reason so the model can relay it.</summary>
    public static async Task<Opened> OpenAsync(AiToolContext context, string sourceId, CancellationToken ct)
    {
        var roles = context.User.Roles;
        var db = context.Db;

        if (sourceId.StartsWith(ContractPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (!ProjectContractRoles.AllowedToReadContract.IncludesAny(roles)) return Refuse("the project contract");
            var projectId = sourceId[ContractPrefix.Length..];
            var contract = await db.ProjectContracts.AsNoTracking().FirstOrDefaultAsync(row => row.ProjectId == projectId, ct);
            if (contract?.DocumentBlobRef is not { Length: > 0 })
                return new Opened(null, null, null, "No contract document has been uploaded for that project — its terms may still be keyed (get_project_contract).");
            var store = context.Services.GetRequiredService<IProjectContractBlobStore>();
            var blob = await store.OpenAsync(contract.DocumentBlobRef, ct);
            return await FromStreamAsync(blob?.Content, blob?.Length, contract.DocumentFileName ?? "contract", Prefer(contract.DocumentContentType, blob?.ContentType), ct);
        }

        if (sourceId.StartsWith(AmendmentPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (!ProjectContractRoles.AllowedToReadContract.IncludesAny(roles)) return Refuse("contract amendments");
            var id = sourceId[AmendmentPrefix.Length..];
            var amendment = await db.ProjectContractAmendments.AsNoTracking().FirstOrDefaultAsync(row => row.ProjectContractAmendmentId == id, ct);
            if (amendment is null) return new Opened(null, null, null, $"No contract amendment exists with id \"{id}\".");
            var store = context.Services.GetRequiredService<IProjectContractBlobStore>();
            var blob = await store.OpenAsync(amendment.DocumentBlobRef, ct);
            return await FromStreamAsync(blob?.Content, blob?.Length, amendment.DocumentFileName, Prefer(amendment.DocumentContentType, blob?.ContentType), ct);
        }

        if (sourceId.StartsWith(InstructionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (!ArchitectInstructionRoles.AllowedToRead.IncludesAny(roles)) return Refuse("Architect's Instructions");
            var id = sourceId[InstructionPrefix.Length..];
            var instruction = await db.ArchitectInstructions.AsNoTracking().FirstOrDefaultAsync(row => row.ArchitectInstructionId == id, ct);
            if (instruction is null) return new Opened(null, null, null, $"No Architect's Instruction exists with id \"{id}\".");
            if (string.IsNullOrWhiteSpace(instruction.BlobRef))
                return new Opened(null, null, null, $"{instruction.Reference} is a placeholder — the instruction's paperwork has not been filed yet, so there is nothing to read.");
            var store = context.Services.GetRequiredService<IArchitectInstructionBlobStore>();
            var blob = await store.OpenAsync(instruction.BlobRef, ct);
            return await FromStreamAsync(blob?.Content, blob?.Length, instruction.FileName ?? instruction.Reference, Prefer(instruction.ContentType, blob?.ContentType), ct);
        }

        if (sourceId.StartsWith(DrawingPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (!JpmsRoleSets.DrawingReaders.IncludesAny(roles)) return Refuse("drawings");
            var id = sourceId[DrawingPrefix.Length..];
            var revision = await db.DrawingRevisions.AsNoTracking().FirstOrDefaultAsync(row => row.DrawingRevisionId == id, ct);
            if (revision is null) return new Opened(null, null, null, $"No drawing revision exists with id \"{id}\".");
            if (string.IsNullOrWhiteSpace(revision.BlobRef))
                return new Opened(null, null, null, $"Revision {revision.RevisionLabel} of that drawing has no stored file.");
            var store = context.Services.GetRequiredService<IDrawingBlobStore>();
            var blob = await store.OpenAsync(revision.BlobRef, ct);
            return await FromStreamAsync(blob?.Content, blob?.Length, revision.FileName, Prefer(revision.ContentType, blob?.ContentType), ct);
        }

        if (sourceId.StartsWith(CertificatePrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (!DocumentControlRoles.AllowedToReadPaymentCertificates.IncludesAny(roles)) return Refuse("payment certificates");
            var id = sourceId[CertificatePrefix.Length..];
            var certificate = await db.PaymentCertificates.AsNoTracking().FirstOrDefaultAsync(row => row.PaymentCertificateId == id, ct);
            if (certificate is null) return new Opened(null, null, null, $"No payment certificate exists with id \"{id}\".");
            var store = context.Services.GetRequiredService<IDocumentControlBlobStore>();
            var blob = await store.OpenAsync(certificate.BlobRef, ct);
            return await FromStreamAsync(blob?.Content, blob?.Length, certificate.FileName, Prefer(certificate.ContentType, blob?.ContentType), ct);
        }

        if (sourceId.StartsWith(DocumentControlPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (!DocumentControlRoles.AllowedToManage.IncludesAny(roles)) return Refuse("Document Control files");
            var id = sourceId[DocumentControlPrefix.Length..];
            var item = await db.DocumentControlItems.AsNoTracking().FirstOrDefaultAsync(row => row.DocumentControlItemId == id, ct);
            if (item is null) return new Opened(null, null, null, $"No Document Control item exists with id \"{id}\".");
            var store = context.Services.GetRequiredService<IDocumentControlBlobStore>();
            var blob = await store.OpenAsync(item.BlobRef, ct);
            return await FromStreamAsync(blob?.Content, blob?.Length, item.FileName, Prefer(item.ContentType, blob?.ContentType), ct);
        }

        if (sourceId.StartsWith(CompliancePrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (!ComplianceReaders.IncludesAny(roles)) return Refuse("compliance files");
            var id = sourceId[CompliancePrefix.Length..];
            var file = await db.ComplianceDocuments.AsNoTracking().FirstOrDefaultAsync(row => row.ComplianceDocumentId == id, ct);
            if (file is null) return new Opened(null, null, null, $"No compliance document exists with id \"{id}\".");
            if (string.IsNullOrWhiteSpace(file.BlobPath))
                return new Opened(null, null, null, $"«{file.Kind}» was recorded before files were stored — there is no file to read.");
            var store = context.Services.GetRequiredService<IComplianceBlobStore>();
            var blob = await store.OpenAsync(file.BlobPath, ct);
            return await FromStreamAsync(blob?.Content, blob?.Length, file.FileName, Prefer(file.ContentType, blob?.ContentType), ct);
        }

        if (sourceId.StartsWith(TenderEnquiryPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (!JpmsRoleSets.AllInternal.IncludesAny(roles)) return Refuse("tender enquiry documents");
            var id = sourceId[TenderEnquiryPrefix.Length..];
            var file = await db.TenderEnquiryAttachments.AsNoTracking().FirstOrDefaultAsync(row => row.TenderEnquiryAttachmentId == id, ct);
            if (file is null) return new Opened(null, null, null, $"No tender enquiry document exists with id \"{id}\" — get_tender_enquiry_context lists them.");
            var store = context.Services.GetRequiredService<ITenderEnquiryAttachmentStore>();
            var blob = await store.OpenAsync(file.BlobRef, ct);
            return await FromStreamAsync(blob?.Content, blob?.Length, file.FileName, Prefer(file.ContentType, blob?.ContentType), ct);
        }

        return new Opened(null, null, null, $"\"{sourceId}\" is not a filed-document handle.");
    }

    private static Opened Refuse(string what) =>
        new(null, null, null, $"The user's role cannot read {what} in the portal, so the assistant cannot read them either.");

    /// <summary>The store hands back a stream (or null when the blob is gone, or when no storage
    /// is configured — the stores cannot tell the two apart); the reader wants bytes.</summary>
    private static async Task<Opened> FromStreamAsync(Stream? content, long? length, string fileName, string? contentType, CancellationToken ct)
    {
        if (content is null)
            return new Opened(null, fileName, contentType, $"\"{fileName}\" could not be fetched from storage — the file is missing, or file storage is not configured on this environment.");
        try
        {
            using (content)
            {
                // Refused on the store's reported length BEFORE the download — a 200 MB plotted
                // drawing must not be pulled into memory to be told it is too big.
                if (length > AiAttachmentReader.MaxBytes)
                {
                    return new Opened(null, fileName, contentType, $"\"{fileName}\" is {length / 1_048_576.0:0.#} MB — too big to read here. "
                        + "Tell the user which file holds the answer and ask them to open it themselves.");
                }
                using var buffer = new MemoryStream();
                await content.CopyToAsync(buffer, ct);
                if (buffer.Length > AiAttachmentReader.MaxBytes)
                {
                    return new Opened(null, fileName, contentType, $"\"{fileName}\" is {buffer.Length / 1_048_576.0:0.#} MB — too big to read here. "
                        + "Tell the user which file holds the answer and ask them to open it themselves.");
                }
                return new Opened(buffer.ToArray(), fileName, contentType, null);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new Opened(null, fileName, contentType, $"\"{fileName}\" could not be fetched from storage ({ex.Message}).");
        }
    }

    /// <summary>The content type as the endpoints resolve it: the record's own when it has one,
    /// the store's otherwise.</summary>
    private static string? Prefer(string? recorded, string? stored) =>
        string.IsNullOrWhiteSpace(recorded) ? stored : recorded;

    private static bool Readable(string? fileName, string? contentType) =>
        !string.IsNullOrWhiteSpace(fileName) && AiSourceReader.IsSupported(fileName, contentType);
}
