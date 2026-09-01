using Ganss.Xss;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.RecordLinks;
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
                        + "todo, lad, cost_centre, scheduling, subcontractor_comms, valuation_snapshot, tender_enquiry. "
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
                            + "cost_centre, scheduling, subcontractor_comms, valuation_snapshot, tender_enquiry.");
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

                    // A wall clock over the whole per-message Graph fan-out. These fetches are
                    // sequential and each carries the shared HttpClient's ~100s default; a hop is
                    // one Claude call (up to 36s) PLUS this, under one ~45s gateway, so an
                    // unbounded loop on a slow mailbox took the whole turn past the ceiling and
                    // cost the user a 502. Expiring here costs the OLDER emails their bodies
                    // (they fall back to headline + preview below) — the same trade
                    // RequestContextAssembler makes.
                    var fetchClock = System.Diagnostics.Stopwatch.StartNew();
                    var fetchDeadline = TimeSpan.FromSeconds(8);

                    foreach (var message in messages.OrderByDescending(m => m.ReceivedAt))
                    {
                        if (budget <= 0 || fetchClock.Elapsed > fetchDeadline) break;
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

                    // The replies the record's page is blind to (its amber "newer replies aren't
                    // filed yet" banner) — the SAME read the banner makes, so a connector session
                    // sees the gap without needing the user's mailbox connected, and can offer
                    // the file_unfiled_replies action. Best-effort: a throttled Graph read must
                    // not cost the tagged list above.
                    IReadOnlyList<MailboxMessage> unfiled = Array.Empty<MailboxMessage>();
                    try
                    {
                        var unfiledReader = context.Services
                            .GetRequiredService<IQueryHandler<ListUnfiledReplies, IReadOnlyList<MailboxMessage>>>();
                        unfiled = await unfiledReader.HandleAsync(new ListUnfiledReplies(recordType, recordId!), ct);
                    }
                    catch (Exception) when (!ct.IsCancellationRequested) { /* best-effort aside */ }

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
                        unfiledReplies = unfiled.Count == 0
                            ? null
                            : unfiled.Select(reply => new
                            {
                                messageId = reply.Id,
                                from = string.IsNullOrWhiteSpace(reply.FromName) ? reply.FromEmail : reply.FromName,
                                fromEmail = reply.FromEmail,
                                reply.Subject,
                                received = reply.ReceivedAt,
                                preview = reply.BodyPreview
                            }).ToList<object>(),
                        note = "Oldest first. Attachment ids feed read_email_attachment. Nothing here "
                               + "extracts figures for you — read the bodies and quote only what they say."
                               + (unfiled.Count == 0
                                   ? ""
                                   : $" unfiledReplies lists {unfiled.Count} newer thread "
                                     + "member(s) NOT yet filed to this record (the page's amber banner) "
                                     + "— tell the user, and offer the file_unfiled_replies action to "
                                     + "file them all.")
                    });
                }),

            new(
                "read_email_attachment",
                "One attachment from an email you have read, by the messageId and attachment id "
                + "read_record_emails or get_mailbox_message returned — the same as read_source with "
                + "source_id mail:<messageId>|<attachmentId>, reading from the start. Prefer "
                + "read_source: it reads a NAMED sheet or page and pages through a long file, and "
                + "find_in_source finds where a reference appears first. Every standard format opens "
                + "(spreadsheets as displayed values, PDFs by page, Word documents, text files) and an "
                + "IMAGE is SHOWN to you on your next step. What genuinely cannot be read is refused "
                + "with the reason — relay it and ask the user rather than guessing.",
                AiToolSchema.Object(
                    ("messageId", "string",
                        "The email's messageId from read_record_emails or get_mailbox_message.", true),
                    ("attachmentId", "string", "The attachment's id from the same tool result.", true),
                    ("maxChars", "number",
                        "How much extracted text to return. Default 20000, minimum 2000, maximum "
                        + "50000. The result says where it stopped; continue with read_source.", false)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var messageId = AiToolSchema.Text(input, "messageId");
                    var attachmentId = AiToolSchema.Text(input, "attachmentId");
                    if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(attachmentId))
                        return Fail("Both messageId and attachmentId are required — read_record_emails returns them.");

                    var limit = Math.Clamp(AiToolSchema.Number(input, "maxChars") ?? Sources.AiSourceReader.DefaultReadChars,
                        Sources.AiSourceReader.MinReadChars, Sources.AiSourceReader.MaxReadChars);
                    return await AiSourceTools.ReadAsync(
                        context, AiSourceTools.MailSourceId(messageId!, attachmentId!), null, 1, limit, ct);
                }),

            new(
                "get_bid_package_context",
                "Everything held ON a bid package record, in one call: title, trade, status, the "
                + "specification summary, the current line-item schedule (with cost codes and "
                + "coverage), who is on the tender list, and the names of its tender documents and "
                + "linked drawings. Call this FIRST when building a package out or answering "
                + "questions about one; the tagged emails are separate — read_record_emails "
                + "(record_type bid_package) has those, and read_email_attachment opens their files. "
                + "Defaults to the bid package on the page in view.",
                AiToolSchema.Object(
                    ("bidPackageId", "string",
                        "The bid package's id. Defaults to the record in view when the user is on "
                        + "its page.", false)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var bidPackageId = AiToolSchema.Text(input, "bidPackageId")
                        ?? (TryMapRecordType(context.Scope?.RecordType ?? "", out var scopeType)
                            && scopeType == RecordType.BidPackageInvite
                            ? context.Scope?.RecordId : null);
                    if (string.IsNullOrWhiteSpace(bidPackageId))
                        return Fail("Say which bid package: pass bidPackageId, or have the user open its page.");

                    var package = await context.Db.BidPackages.AsNoTracking()
                        .FirstOrDefaultAsync(row => row.BidPackageId == bidPackageId, ct);
                    if (package is null) return Fail($"No bid package found with id {bidPackageId}.");

                    var lines = await context.Db.BidPackageLineItems.AsNoTracking()
                        .Where(row => row.BidPackageId == bidPackageId)
                        .OrderBy(row => row.SortOrder)
                        .Select(row => new
                        {
                            row.Trade,
                            row.Description,
                            row.Unit,
                            row.Quantity,
                            row.CostCode,
                            coverage = ((BidPackageLineCoverage)row.Coverage).ToString()
                        })
                        .ToListAsync(ct);

                    var recipients = await (
                        from recipient in context.Db.BidPackageRecipients.AsNoTracking()
                        where recipient.BidPackageId == bidPackageId
                        join sub in context.Db.Subcontractors.AsNoTracking()
                            on recipient.SubcontractorId equals sub.SubcontractorId into subs
                        from sub in subs.DefaultIfEmpty()
                        select new
                        {
                            company = sub != null ? sub.CompanyName : recipient.SubcontractorId,
                            status = ((BidPackageRecipientStatus)recipient.Status).ToString()
                        })
                        .ToListAsync(ct);

                    var attachments = await context.Db.BidPackageAttachments.AsNoTracking()
                        .Where(row => row.BidPackageId == bidPackageId)
                        .Select(row => new { row.FileName, row.ContentType })
                        .ToListAsync(ct);

                    var drawingCount = await context.Db.BidPackageDrawings.AsNoTracking()
                        .CountAsync(row => row.BidPackageId == bidPackageId, ct);

                    return Serialise(new
                    {
                        ok = true,
                        reference = package.Reference,
                        package.Title,
                        package.Trade,
                        status = ((BidPackageStatus)package.Status).ToString(),
                        package.MaterialsApplicable,
                        specificationSummary = package.SpecificationSummary,
                        lineItems = lines,
                        tenderList = recipients,
                        tenderDocuments = attachments,
                        linkedDrawings = drawingCount,
                        note = "Tagged emails are separate — read_record_emails (record_type "
                               + "bid_package) returns them with full bodies and attachment ids."
                    });
                }),

            new(
                "get_work_order_context",
                "Everything held ON a work order record, in one call: reference, status, origin "
                + "(manual, tender award, variation instruction, or migrated), supplier, title, "
                + "scope, order value, the priced lines (each with its cost code and the amount "
                + "already PAID against it — a paid line can never be removed and never priced "
                + "below what is paid), programme dates and the names of its record-keeping "
                + "attachments. Call this FIRST when editing an order (work_order_edit) or "
                + "answering questions about one; the tagged emails are separate — "
                + "read_record_emails (record_type work_order) has those, and "
                + "read_email_attachment opens their files. A DRAFT has no number yet — its "
                + "workOrderId comes from list_work_orders (status Draft), never from a guessed "
                + "reference. Accepts the id, or the reference the "
                + "user actually says (\"WO-0045\") with the project resolved from the page in "
                + "view. Defaults to the work order in view.",
                AiToolSchema.Object(
                    ("workOrderId", "string",
                        "The work order's id. Defaults to the record in view when the user is on "
                        + "its PO page.", false),
                    ("reference", "string",
                        "The human reference instead — \"WO-0045\" (or just \"45\"). Resolved "
                        + "against the project in view or projectId.", false),
                    ("projectId", "string",
                        "The project a reference is resolved in. Defaults to the project in view.", false)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var workOrderId = AiToolSchema.Text(input, "workOrderId")
                        ?? (TryMapRecordType(context.Scope?.RecordType ?? "", out var scopeType)
                            && scopeType == RecordType.WorkOrder
                            ? context.Scope?.RecordId : null);

                    Data.Entities.WorkOrderEntity? order = null;
                    if (!string.IsNullOrWhiteSpace(workOrderId))
                    {
                        order = await context.Db.WorkOrders.AsNoTracking()
                            .FirstOrDefaultAsync(row => row.WorkOrderId == workOrderId, ct);
                        if (order is null) return Fail($"No work order found with id {workOrderId}.");
                    }
                    else if (AiToolSchema.Text(input, "reference") is { } reference && !string.IsNullOrWhiteSpace(reference))
                    {
                        // "WO-0045" → 45. The number is unique per project, so a reference needs one.
                        var digits = new string(reference.Where(char.IsDigit).ToArray());
                        if (digits.Length == 0 || !int.TryParse(digits, out var number))
                            return Fail($"\"{reference}\" doesn't contain an order number — say it like WO-0045.");
                        var projectId = AiToolSchema.Text(input, "projectId") ?? context.Scope?.ProjectId;
                        if (string.IsNullOrWhiteSpace(projectId))
                            return Fail("Say which project the reference belongs to: pass projectId, or have the user open a page of that project.");
                        order = await context.Db.WorkOrders.AsNoTracking()
                            .FirstOrDefaultAsync(row => row.ProjectId == projectId && row.Number == number, ct);
                        if (order is null) return Fail($"No work order numbered {number} on project {projectId}.");
                    }
                    else
                    {
                        return Fail("Say which work order: pass workOrderId or a reference like WO-0045.");
                    }

                    var lines = await context.Db.WorkOrderLines.AsNoTracking()
                        .Where(row => row.WorkOrderId == order.WorkOrderId)
                        .OrderBy(row => row.SortOrder)
                        .Select(row => new
                        {
                            row.Title,
                            row.Description,
                            row.CostCode,
                            amount = row.LineTotal,
                            paidToDate = row.PaidToDate
                        })
                        .ToListAsync(ct);

                    var supplier = await context.Db.Subcontractors.AsNoTracking()
                        .FirstOrDefaultAsync(row => row.SubcontractorId == order.SubcontractorId, ct);

                    var attachments = await context.Db.WorkOrderAttachments.AsNoTracking()
                        .Where(row => row.WorkOrderId == order.WorkOrderId)
                        .Select(row => new { row.FileName, row.ContentType })
                        .ToListAsync(ct);

                    return Serialise(new
                    {
                        ok = true,
                        workOrderId = order.WorkOrderId,
                        projectId = order.ProjectId,
                        reference = order.Reference,
                        status = ((WorkOrderStatus)order.Status).ToString(),
                        origin = order.BidPackageId is not null ? "tender award"
                            : order.VariationOrderId is not null ? "variation instruction"
                            : order.SourceReference is not null ? "migrated"
                            : "manual",
                        supplier = supplier?.CompanyName ?? order.SubcontractorId,
                        // The directory record's id, so a session can go straight to
                        // search_directory / update_subcontractor without guessing (2026-08-29:
                        // the accountant's session had the name but no id, invented one, and
                        // misread the resulting "not found" as a broken record).
                        subcontractorId = order.SubcontractorId,
                        order.Title,
                        order.Scope,
                        value = order.Value,
                        programmeStart = order.ProgrammeStart,
                        targetCompletion = order.ScheduledCompletion,
                        programmeNotes = order.ProgrammeNotes,
                        depositRequired = order.DepositRequired,
                        depositPercent = order.DepositPercent,
                        lines,
                        attachments,
                        note = "Tagged emails are separate — read_record_emails (record_type "
                               + "work_order) returns them with full bodies and attachment ids. "
                               + "Editing: open_modal work_order_edit with this workOrderId as "
                               + "record_id; a line with paidToDate ≠ 0 can't be removed or "
                               + "priced below that figure. subcontractorId is the supplier's "
                               + "directory record — search_directory reads it, "
                               + "update_subcontractor edits it (address, contact, terms)."
                    });
                }),

            new(
                "search_directory",
                "Finds company records in the subcontractor/supplier directory: search by name (or "
                + "contact name/email), optionally narrowed to a category. Returns each match's "
                + "subcontractorId — the id update_subcontractor and the procurement actions need — "
                + "with its category, trades (ids and names, exactly what update_subcontractor must "
                + "send back in full), primary contact, postal address, CIS status, payment terms, "
                + "whether it is linked to a Xero contact, and whether it is still a tender-only "
                + "prospect (promote_subcontractor_to_directory makes those permanent). Call this "
                + "BEFORE update_subcontractor or add_subcontractor_to_directory — never guess an "
                + "id, and never create a record before checking it isn't already here.",
                AiToolSchema.Object(
                    ("search", "string",
                        "Text matched against company name, contact name and contact email — "
                        + "\"Sussex Tiling\", \"jo@acme\". Left out, the directory lists from the "
                        + "top (capped, so search when you know the name).", false),
                    ("category", "string",
                        "Optional filter: Subcontractor, Client, Architect, Supplier or Other.", false)),
                AiToolKind.Read,
                // Mirrors ListSubcontractorsEndpoint.InternalRolesThatMayListDirectory (the full
                // directory with contact details is internal-only; external sessions get their own
                // scoped views), plus Role.Admin named explicitly per the authorisation convention.
                RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
                    JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.SiteManager,
                    JpmsRoles.HealthAndSafetyLead, JpmsRoles.OfficeComplianceCoordinator,
                    JpmsRoles.OfficeAdmin, JpmsRoles.Foreman),
                async (context, input, ct) =>
                {
                    var search = AiToolSchema.Text(input, "search");
                    var categoryText = AiToolSchema.Text(input, "category");

                    var query = context.Db.Subcontractors.AsNoTracking();
                    if (!string.IsNullOrWhiteSpace(search))
                        query = query.Where(row => row.CompanyName.Contains(search)
                            || row.ContactName.Contains(search)
                            || row.ContactEmail.Contains(search));
                    if (!string.IsNullOrWhiteSpace(categoryText))
                    {
                        if (!Enum.TryParse<DirectoryCategory>(categoryText, ignoreCase: true, out var category))
                            return Fail("category must be Subcontractor, Client, Architect, Supplier or Other.");
                        query = query.Where(row => row.Category == (int)category);
                    }

                    // Capped like every listing tool — the fix for a truncated result is a better
                    // search term, not a bigger dump.
                    const int cap = 25;
                    var rows = await query.OrderBy(row => row.CompanyName)
                        .Take(cap + 1).ToListAsync(ct);
                    var truncated = rows.Count > cap;
                    if (truncated) rows = rows.Take(cap).ToList();

                    var ids = rows.Select(row => row.SubcontractorId).ToList();
                    var tradeLinks = await (
                        from link in context.Db.SubcontractorTrades.AsNoTracking()
                        join trade in context.Db.Trades.AsNoTracking() on link.TradeId equals trade.TradeId
                        where ids.Contains(link.SubcontractorId)
                        select new { link.SubcontractorId, trade.TradeId, trade.Name })
                        .ToListAsync(ct);
                    var xeroLinked = (await context.Db.SubcontractorXeroLinks.AsNoTracking()
                        .Where(link => ids.Contains(link.SubcontractorId))
                        .Select(link => link.SubcontractorId)
                        .Distinct().ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var companies = rows.Select(row => new
                    {
                        subcontractorId = row.SubcontractorId,
                        companyName = row.CompanyName,
                        category = ((DirectoryCategory)row.Category).ToString(),
                        isProspect = row.IsProspect,
                        trades = tradeLinks.Where(link => link.SubcontractorId == row.SubcontractorId)
                            .Select(link => new { tradeId = link.TradeId, name = link.Name })
                            .OrderBy(trade => trade.name).ToList(),
                        contactName = row.ContactName,
                        contactEmail = row.ContactEmail,
                        contactPhone = row.ContactPhone,
                        mobileNumber = row.MobileNumber,
                        address = new { row.AddressLine, row.Town, row.County, row.Postcode },
                        cisStatus = row.CisStatus,
                        paymentTermsDays = row.PaymentTermsDays,
                        xeroLinked = xeroLinked.Contains(row.SubcontractorId)
                    }).ToList();

                    return Serialise(new
                    {
                        ok = true,
                        count = companies.Count,
                        truncated,
                        companies,
                        note = "subcontractorId is what update_subcontractor takes; send its FULL "
                               + "trades list back when updating — removing the last trade is "
                               + "refused. The address here is what the purchase order prints. "
                               + "Xero-linked records copied their address from Xero at import "
                               + "only — later Xero edits never flow back, so the directory is "
                               + "corrected here."
                               + (truncated ? " More records matched than shown — narrow the search." : "")
                    });
                }),
        };
    }

    /// <summary>The model's (or the route's) name for a record type onto the enum the record-link
    /// layer keys on. Tolerant of spacing and underscores; strict about meaning — an unknown name
    /// fails rather than guessing. Internal so AiTurnRunner's stage_triage_tag validation speaks
    /// the same vocabulary — one mapping, not two that drift.</summary>
    internal static bool TryMapRecordType(string value, out RecordType recordType)
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
            "calendar event" or "calendar" or "event" => RecordType.CalendarEvent,
            "lad" or "liquidated damages" => RecordType.Lad,
            "cost centre" or "cost center" => RecordType.CostCentre,
            "scheduling" or "programme" => RecordType.Scheduling,
            "subcontractor comms" => RecordType.SubcontractorComms,
            "supplier comms" => RecordType.SupplierComms,
            "valuation snapshot" or "valuation report snapshot" => RecordType.ValuationReportSnapshot,
            "tender enquiry" or "tender inquiry" or "teq" => RecordType.TenderEnquiry,
            _ => null
        };
        recordType = mapped ?? default;
        return mapped is not null;
    }
}
