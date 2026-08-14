using System.Text;
using System.Text.Json;
using Ganss.Xss;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Api.Features.Agents;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// Tools that reach into a single record's substance — its correspondence and its attachments — and
/// the one tool that writes back into a form the user has open.
///
/// <para>The attachment pair is deliberately a negotiation rather than a dump. Listing is cheap and
/// returns names only; the model decides what it actually needs and asks for that. Pushing every
/// email body and every attachment into the prompt would cost a fortune and bury the answer.</para>
/// </summary>
internal static class AiRecordTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    public static IReadOnlyList<AiTool> Build()
    {
        var readers = JpmsRoleSets.AllInternal;

        return new List<AiTool>
        {
            new(
                "list_request_correspondence",
                "The emails and attachments on a request, as a list of headlines — sender, subject, date, "
                + "and the names of anything attached. Cheap. Call this FIRST when you need to know what "
                + "exists, then call get_request_context only if you actually need the wording.",
                AiToolSchema.Object(("requestId", "string", "The request's id, from list_requests or find_by_reference.", true)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var requestId = AiToolSchema.Text(input, "requestId");
                    if (string.IsNullOrWhiteSpace(requestId)) return Fail("A requestId is required.");

                    var request = await context.Db.Requests.AsNoTracking()
                        .FirstOrDefaultAsync(row => row.RequestId == requestId, ct);
                    if (request is null) return Fail($"No request found with id {requestId}.");

                    // Files and drawing links held against the record itself.
                    var attachments = await context.Db.RequestAttachments.AsNoTracking()
                        .Where(row => row.RequestId == requestId)
                        .Select(row => new
                        {
                            row.RequestAttachmentId,
                            kind = row.Kind == (int)RequestAttachmentKind.Drawing ? "drawing" : "file",
                            name = row.Kind == (int)RequestAttachmentKind.Drawing
                                ? (row.DrawingCode ?? "") + " " + (row.RevisionLabel ?? "")
                                : (row.FileName ?? ""),
                            row.ContentType,
                            row.Caption
                        })
                        .ToListAsync(ct);

                    // The live mailbox thread. Headlines only — HasAttachments is a flag, not names,
                    // because names cost a per-message Graph round trip.
                    var emails = Array.Empty<object>() as IReadOnlyList<object>;
                    try
                    {
                        var reader = context.Services.GetRequiredService<RequestEmailReader>();
                        var messages = await reader.ForRequestAsync(requestId, ct);
                        emails = messages
                            .Select(message => (object)new
                            {
                                from = string.IsNullOrWhiteSpace(message.FromName) ? message.FromEmail : message.FromName,
                                message.Subject,
                                received = message.ReceivedAt,
                                message.HasAttachments,
                                preview = message.BodyPreview
                            })
                            .ToList();
                    }
                    catch (Exception ex)
                    {
                        return Serialise(new
                        {
                            ok = true,
                            request = request.Reference,
                            attachments,
                            emails = Array.Empty<object>(),
                            note = $"The mailbox could not be read ({ex.Message}). The attachments listed are the "
                                   + "ones held on the record itself."
                        });
                    }

                    return Serialise(new
                    {
                        ok = true,
                        request = request.Reference,
                        request.Title,
                        attachments,
                        emails,
                        note = "Previews are truncated. Call get_request_context for the full wording."
                    });
                }),

            new(
                "read_record_emails",
                "Every email tagged to a record — what its page shows as Communications or Tender "
                + "responses & related emails — with FULL bodies flattened to text, plus each "
                + "attachment's name and id. Works for ANY record type: bid packages, variations, "
                + "requests, work orders, defects, to-dos. This is the tool when the user says \"read "
                + "the emails\": tender line items, what a subcontractor quoted, who said what — it "
                + "all lives here. Defaults to the record on the page in view; nothing else needs "
                + "calling first. Not for requests you are drafting a variation from — "
                + "get_request_context is richer there.",
                AiToolSchema.Object(
                    ("recordType", "string",
                        "One of: request, bid_package, variation, variation_quote, work_order, defect, "
                        + "todo, lad, cost_centre, scheduling, subcontractor_comms, valuation_snapshot. "
                        + "Defaults to the record in view.", false),
                    ("recordId", "string", "The record's id. Defaults to the record in view.", false),
                    ("maxChars", "number",
                        "Total body budget. Default 25000, ceiling 50000. Newest emails keep their "
                        + "bodies first when it runs out; older ones fall back to headlines.", false)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var typeText = AiToolSchema.Text(input, "recordType") ?? context.Scope?.RecordType;
                    var recordId = AiToolSchema.Text(input, "recordId") ?? context.Scope?.RecordId;
                    if (string.IsNullOrWhiteSpace(typeText) || string.IsNullOrWhiteSpace(recordId))
                        return Fail("Say which record: pass recordType and recordId, or have the user open the record's page.");

                    if (!TryMapRecordType(typeText, out var recordType))
                    {
                        return Fail($"Emails cannot be read for \"{typeText}\" — tagged mail exists for: request, "
                            + "bid_package, variation, variation_quote, work_order, defect, todo, lad, "
                            + "cost_centre, scheduling, subcontractor_comms, valuation_snapshot.");
                    }

                    IReadOnlyList<MailboxMessage> messages;
                    try
                    {
                        var emailReader = context.Services.GetRequiredService<RecordEmailReader>();
                        messages = await emailReader.ForRecordAsync(recordType, recordId!, ct);
                    }
                    catch (Exception ex)
                    {
                        return Fail($"The mailbox could not be read ({ex.Message}).");
                    }

                    if (messages.Count == 0)
                    {
                        return Serialise(new
                        {
                            ok = true,
                            emails = Array.Empty<object>(),
                            note = "No emails are tagged to this record — or the record was not found, "
                                   + "or the mailbox is not configured on this environment."
                        });
                    }

                    // Full bodies, newest emails first when the budget bites: on a tender the latest
                    // reply is usually the one carrying the prices. Older emails degrade to their
                    // headline + preview rather than disappearing.
                    var budget = Math.Clamp(AiToolSchema.Number(input, "maxChars") ?? 25_000, 1_000, 50_000);
                    var detailReader = context.Services.GetRequiredService<IIntakeMessageReader>();
                    var bodies = new Dictionary<string, object>(StringComparer.Ordinal);

                    foreach (var message in messages.OrderByDescending(m => m.ReceivedAt))
                    {
                        if (budget <= 0) break;
                        IntakeMessageContent? content;
                        try
                        {
                            content = await detailReader.GetAsync(message.Id, ct);
                        }
                        catch (Exception) when (!ct.IsCancellationRequested)
                        {
                            continue; // One unreadable email must not lose the rest.
                        }
                        if (content is null) continue;

                        var text = content.IsHtml
                            ? RequestContextAssembler.HtmlToText(new HtmlSanitizer().Sanitize(content.Body))
                            : content.Body ?? "";
                        text = text.Trim();

                        var take = Math.Min(text.Length, Math.Min(budget, 8_000));
                        var clipped = take < text.Length;
                        budget -= take;

                        bodies[message.Id] = new
                        {
                            body = clipped
                                ? text[..take] + "\n[… this email was longer and has been cut here.]"
                                : text,
                            attachments = content.Attachments
                                .Select(file => new { file.Id, file.Name, file.Size, file.ContentType })
                                .ToList()
                        };
                    }

                    return Serialise(new
                    {
                        ok = true,
                        emails = messages.Select(message => new
                        {
                            messageId = message.Id,
                            from = string.IsNullOrWhiteSpace(message.FromName) ? message.FromEmail : message.FromName,
                            fromEmail = message.FromEmail,
                            message.Subject,
                            received = message.ReceivedAt,
                            detail = bodies.TryGetValue(message.Id, out var detail)
                                ? detail
                                : (object)new
                                {
                                    body = $"[body omitted — over the {AiToolSchema.Number(input, "maxChars") ?? 25_000}-character budget; "
                                           + $"preview: {message.BodyPreview}]",
                                    attachments = (object)(message.HasAttachments
                                        ? "has attachments — re-call with a higher maxChars to see their names"
                                        : "none")
                                }
                        }).ToList(),
                        note = "Oldest first. Attachment ids feed read_email_attachment. Nothing here "
                               + "extracts figures for you — read the bodies and quote only what they say."
                    });
                }),

            new(
                "read_email_attachment",
                "One attachment from a tagged email, by the messageId and attachment id "
                + "read_record_emails returned. Text-based files (txt, csv, json, xml, html, eml, md) "
                + "come back as text. PDFs, images and spreadsheets cannot be converted to text on "
                + "this environment yet — you will be told so when you try; then name the file that "
                + "holds the answer and ask the user for the figures rather than guessing.",
                AiToolSchema.Object(
                    ("messageId", "string", "The email's messageId from read_record_emails.", true),
                    ("attachmentId", "string", "The attachment's id from read_record_emails.", true)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var messageId = AiToolSchema.Text(input, "messageId");
                    var attachmentId = AiToolSchema.Text(input, "attachmentId");
                    if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(attachmentId))
                        return Fail("Both messageId and attachmentId are required — read_record_emails returns them.");

                    IntakeAttachmentContent? file;
                    try
                    {
                        var detailReader = context.Services.GetRequiredService<IIntakeMessageReader>();
                        file = await detailReader.GetAttachmentAsync(messageId!, attachmentId!, ct);
                    }
                    catch (Exception ex)
                    {
                        return Fail($"The attachment could not be fetched ({ex.Message}).");
                    }

                    if (file is null)
                        return Fail("That attachment could not be fetched — it may be an attached email or a link rather than a file.");

                    if (!IsTextLike(file.Name, file.ContentType))
                    {
                        // ADR-007: declared, not hidden. A structured refusal the model can relay
                        // honestly beats silently omitting the capability.
                        return Serialise(new
                        {
                            ok = false,
                            kind = "not_supported",
                            error = $"\"{file.Name}\" is {file.ContentType} — this environment cannot extract text "
                                    + "from that format yet. Tell the user the answer appears to be in this file, "
                                    + "and either ask them for the figures or work from the email bodies.",
                            operatorAction = "Add server-side text extraction (PDF/spreadsheet) behind read_email_attachment."
                        });
                    }

                    const int MaxAttachmentChars = 20_000;
                    string text;
                    try
                    {
                        text = Encoding.UTF8.GetString(file.Content);
                    }
                    catch (Exception)
                    {
                        return Fail($"\"{file.Name}\" could not be decoded as text.");
                    }

                    if (LooksLikeHtml(file.Name, file.ContentType))
                        text = RequestContextAssembler.HtmlToText(new HtmlSanitizer().Sanitize(text));

                    var clipped = text.Length > MaxAttachmentChars;
                    if (clipped) text = text[..MaxAttachmentChars];

                    return Serialise(new
                    {
                        ok = true,
                        file.Name,
                        file.ContentType,
                        content = text,
                        truncated = clipped
                    });
                }),
        };
    }

    /// <summary>The model's (or the route's) name for a record type onto the enum the record-link
    /// layer keys on. Tolerant of spacing and underscores; strict about meaning — an unknown name
    /// fails rather than guessing.</summary>
    private static bool TryMapRecordType(string value, out RecordType recordType)
    {
        var normalised = value.Trim().ToLowerInvariant().Replace('-', ' ').Replace('_', ' ');
        RecordType? mapped = normalised switch
        {
            "request" or "rfi" or "rfa" or "rfc" or "rfq" or "rfp" or "nod" or "eot" => RecordType.Request,
            "bid package" or "bid package invite" or "bidpackage" or "bpi" => RecordType.BidPackageInvite,
            "variation" or "variation order" or "vo" => RecordType.Variation,
            "variation quote" or "voq" => RecordType.VariationQuote,
            "work order" or "purchase order" or "po" => RecordType.WorkOrder,
            "defect" => RecordType.Defect,
            "todo" or "to do" => RecordType.Todo,
            "lad" or "liquidated damages" => RecordType.Lad,
            "cost centre" or "cost center" => RecordType.CostCentre,
            "scheduling" or "programme" => RecordType.Scheduling,
            "subcontractor comms" => RecordType.SubcontractorComms,
            "valuation snapshot" or "valuation report snapshot" => RecordType.ValuationReportSnapshot,
            _ => null
        };
        recordType = mapped ?? default;
        return mapped is not null;
    }

    private static readonly string[] TextExtensions =
        { ".txt", ".csv", ".md", ".json", ".xml", ".htm", ".html", ".eml", ".log" };

    private static bool IsTextLike(string name, string? contentType)
    {
        if (contentType is not null)
        {
            var type = contentType.ToLowerInvariant();
            if (type.StartsWith("text/", StringComparison.Ordinal)) return true;
            if (type.Contains("json") || type.Contains("xml") || type.Contains("csv")) return true;
        }
        var extension = System.IO.Path.GetExtension(name).ToLowerInvariant();
        return TextExtensions.Contains(extension);
    }

    private static bool LooksLikeHtml(string name, string? contentType)
    {
        if (contentType?.ToLowerInvariant().Contains("html") == true) return true;
        var extension = System.IO.Path.GetExtension(name).ToLowerInvariant();
        return extension is ".htm" or ".html";
    }
}
