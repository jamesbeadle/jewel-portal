using System.Text.Json;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.ArchitectInstructions;
using Jewel.JPMS.Api.Features.BuildingControl;
using Jewel.JPMS.Api.Features.Calendar;
using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ArchitectInstructions;
using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Contracts.Calendar;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The delivery-side read surface (2026-08-31): the project calendar, building control, the
/// programme with its LAD claims, the Architect's Instruction register, progress updates/reports,
/// the drawing register and the package reconciliation. Each tool wraps the SAME query handler its
/// HTTP endpoint composes and mirrors that endpoint's role gate exactly.
/// </summary>
internal static class AiDeliveryTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    /// <summary>Mirror of GetProgrammeDetailEndpoint.RolesThatMayReadSite and
    /// ListLadClaimsForProjectEndpoint.InternalReadRoles — both are JpmsRoleSets.AllInternal.</summary>
    private static readonly RoleSet ProgrammeReaders = JpmsRoleSets.AllInternal;

    /// <summary>Mirror of ReconciliationPackageQueryEndpoints.InternalReadRoles.</summary>
    private static readonly RoleSet ReconciliationReaders = JpmsRoleSets.AllInternal;

    /// <summary>Mirror of the drawing query endpoints' RolesThatMayReadDrawings
    /// (ListDrawingsForProjectEndpoint / ListDrawingFoldersForProjectEndpoint /
    /// ListRevisionsForDrawingEndpoint) — all JpmsRoleSets.DrawingReaders.</summary>
    private static readonly RoleSet DrawingReaders = JpmsRoleSets.DrawingReaders;

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    private static string? ProjectId(AiToolContext context, JsonElement input) =>
        AiToolSchema.Text(input, "projectId") ?? context.Scope?.ProjectId;

    public static IReadOnlyList<AiTool> Build()
    {
        return new List<AiTool>
        {
            new(
                "list_calendar_events",
                "A project's calendar — every dated event people need to see coming: site visits, "
                + "deliveries, meetings, subcontractor attendances. Each carries its CAL reference "
                + "(also its mailbox tag stem), kind, date (with optional start time and inclusive "
                + "end date for multi-day events), notes, and a clientVisible flag marking the "
                + "client-safe subset — events a client could be shown; the rest are internal.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view; pass it otherwise (list_projects returns ids).", false)),
                AiToolKind.Read,
                CalendarRoles.Readers,
                async (context, input, ct) =>
                {
                    var projectId = ProjectId(context, input);
                    if (string.IsNullOrWhiteSpace(projectId))
                        return Fail("Say which project: pass projectId (list_projects returns ids).");

                    var events = await context.Services
                        .GetRequiredService<IQueryHandler<ListCalendarEventsForProject, IReadOnlyList<CalendarEvent>>>()
                        .HandleAsync(new ListCalendarEventsForProject(projectId), ct);

                    return Serialise(new
                    {
                        ok = true,
                        projectId,
                        count = events.Count,
                        events = events.Select(item => new
                        {
                            item.CalendarEventId,
                            item.Reference,
                            item.Title,
                            kind = item.Kind.ToString(),
                            date = item.Date,
                            startTime = item.StartTime,
                            endDate = item.EndDate,
                            item.Notes,
                            item.ClientVisible,
                            item.CreatedByEmail
                        })
                    });
                }),

            new(
                "get_building_control",
                "A project's building control in one answer: the case(s) with the body — regime "
                + "(local authority or private registered approver), the body's reference, contact, "
                + "case status and official dates — plus the inspection register (BCI refs; status "
                + "ladder Planned → Booked → Inspected → Passed / Actions required → Closed, where "
                + "Actions required re-books the SAME record) and every file on the case or its "
                + "inspections. Cases come newest-first, the active one leading.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false)),
                AiToolKind.Read,
                BuildingControlRoles.Readers,
                async (context, input, ct) =>
                {
                    var projectId = ProjectId(context, input);
                    if (string.IsNullOrWhiteSpace(projectId))
                        return Fail("Say which project: pass projectId (list_projects returns ids).");

                    var view = await context.Services
                        .GetRequiredService<IQueryHandler<GetBuildingControlForProject, BuildingControlProjectView>>()
                        .HandleAsync(new GetBuildingControlForProject(projectId), ct);

                    return Serialise(new
                    {
                        ok = true,
                        projectId,
                        cases = view.Cases.Select(item => new
                        {
                            item.BuildingControlCaseId,
                            item.Reference,
                            regime = item.Regime.ToString(),
                            item.BodyName,
                            item.BodyReference,
                            item.ContactName,
                            item.ContactEmail,
                            status = item.Status.ToString(),
                            item.NoticeSubmittedOn,
                            item.AcceptedOn,
                            item.CompletionCertifiedOn,
                            item.Notes
                        }),
                        inspections = view.Inspections.Select(item => new
                        {
                            item.BuildingControlInspectionId,
                            caseId = item.BuildingControlCaseId,
                            item.Reference,
                            item.StageName,
                            status = item.Status.ToString(),
                            item.BookedFor,
                            item.InspectedAt,
                            item.OutcomeNotes,
                            item.InspectorName
                        }),
                        attachments = view.Attachments.Select(item => new
                        {
                            item.BuildingControlAttachmentId,
                            caseId = item.BuildingControlCaseId,
                            inspectionId = item.BuildingControlInspectionId,
                            kind = item.Kind.ToString(),
                            item.FileName,
                            item.AddedAt
                        })
                    });
                }),

            new(
                "get_programme",
                "The Programme tab in one answer: the live programme (tasks with planned dates and "
                + "percent complete, finish-to-start dependency links with lag) with its baselines — "
                + "immutable snapshots of the whole programme, newest first, the latest being the "
                + "yardstick slippage is measured against — plus the project's Liquidated Damages "
                + "claims (the client's claims for late completion: LAD refs, delay period, days, "
                + "rate, amount, status). A claim's LAD reference is its mailbox tag stem, so tagged "
                + "emails link to it as evidence.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false)),
                AiToolKind.Read,
                ProgrammeReaders,
                async (context, input, ct) =>
                {
                    var projectId = ProjectId(context, input);
                    if (string.IsNullOrWhiteSpace(projectId))
                        return Fail("Say which project: pass projectId (list_projects returns ids).");

                    var detail = await context.Services
                        .GetRequiredService<IQueryHandler<GetProgrammeDetail, ProgrammeDetail>>()
                        .HandleAsync(new GetProgrammeDetail(projectId), ct);
                    var claims = await context.Services
                        .GetRequiredService<IQueryHandler<ListLadClaimsForProject, IReadOnlyList<LadClaim>>>()
                        .HandleAsync(new ListLadClaimsForProject(projectId), ct);

                    return Serialise(new
                    {
                        ok = true,
                        projectId,
                        tasks = detail.Tasks,
                        links = detail.Links,
                        latestBaseline = detail.Baseline,
                        latestBaselineTasks = detail.BaselineTasks,
                        baselines = detail.Baselines,
                        ladClaims = claims.Select(claim => new
                        {
                            claim.LadClaimId,
                            claim.Reference,
                            claim.Title,
                            claim.Description,
                            claim.PeriodFrom,
                            claim.PeriodTo,
                            claim.DaysClaimed,
                            claim.RatePerWeek,
                            claim.Amount,
                            status = claim.Status.ToString(),
                            claim.RaisedAt
                        })
                    });
                }),

            new(
                "list_architect_instructions",
                "A project's Architect's Instruction register — the formal written instructions that "
                + "turn a requested change into work Jewel is entitled to be paid for. Each row: our "
                + "AI reference and the architect's own number, title, the instruction date, who "
                + "issued and filed it, the variations it covers (one instruction routinely covers "
                + "several), and documentAwaited — true means the row is a placeholder still waiting "
                + "for the paperwork. This register is what a variation at Awaiting AI is waiting for.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false)),
                AiToolKind.Read,
                ArchitectInstructionRoles.AllowedToRead,
                async (context, input, ct) =>
                {
                    var projectId = ProjectId(context, input);
                    if (string.IsNullOrWhiteSpace(projectId))
                        return Fail("Say which project: pass projectId (list_projects returns ids).");

                    var instructions = await context.Services
                        .GetRequiredService<IQueryHandler<ListArchitectInstructionsForProject, IReadOnlyList<ArchitectInstruction>>>()
                        .HandleAsync(new ListArchitectInstructionsForProject(projectId), ct);

                    return Serialise(new
                    {
                        ok = true,
                        projectId,
                        count = instructions.Count,
                        instructions = instructions.Select(instruction => new
                        {
                            instruction.ArchitectInstructionId,
                            instruction.Reference,
                            architectsOwnRef = instruction.InstructionRef,
                            display = instruction.DisplayReference,
                            instruction.Title,
                            instruction.Notes,
                            instructedAt = instruction.InstructedAt,
                            receivedAt = instruction.ReceivedAt,
                            instruction.IssuedByEmail,
                            instruction.FiledByEmail,
                            source = instruction.Source.ToString(),
                            documentAwaited = !instruction.HasFile,
                            linkedVariations = instruction.Links.Select(link => new
                            {
                                link.VariationOrderId,
                                link.DisplayNumber,
                                link.Title,
                                status = link.Status.ToString()
                            })
                        })
                    });
                }),

            new(
                "list_progress",
                "A project's progress registers in one answer: the updates (a site manager's dated "
                + "record of works — title, description, work date, weather, photo count) and the "
                + "client-facing reports (title, period, narrative sections, and WHICH updates each "
                + "includes). Reports are assembled FROM existing updates — an update's photos "
                + "illustrate every report that selects it — and a report's PDF regenerates from "
                + "this register on every download, so it always reflects the register as it stands.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false)),
                AiToolKind.Read,
                ProgressRoles.Readers,
                async (context, input, ct) =>
                {
                    var projectId = ProjectId(context, input);
                    if (string.IsNullOrWhiteSpace(projectId))
                        return Fail("Say which project: pass projectId (list_projects returns ids).");

                    var updates = await context.Services
                        .GetRequiredService<IQueryHandler<ListProgressUpdatesForProject, IReadOnlyList<ProgressUpdate>>>()
                        .HandleAsync(new ListProgressUpdatesForProject(projectId), ct);
                    var reports = await context.Services
                        .GetRequiredService<IQueryHandler<ListProgressReportsForProject, IReadOnlyList<ProgressReport>>>()
                        .HandleAsync(new ListProgressReportsForProject(projectId), ct);

                    return Serialise(new
                    {
                        ok = true,
                        projectId,
                        updates = updates.Select(update => new
                        {
                            update.ProgressUpdateId,
                            update.Title,
                            update.Description,
                            update.WorkDate,
                            weather = update.Weather?.Summary,
                            photoCount = update.Photos.Count,
                            update.CreatedByEmail,
                            update.CreatedAt
                        }),
                        reports = reports.Select(report => new
                        {
                            report.ProgressReportId,
                            report.Title,
                            report.PeriodStart,
                            report.PeriodEnd,
                            report.Introduction,
                            report.WorkCompleted,
                            report.UpcomingWorks,
                            report.CreatedByEmail,
                            report.CreatedAt,
                            includedUpdateIds = report.SelectedUpdateIds
                        })
                    });
                }),

            new(
                "list_drawings",
                "A project's drawing register: the folder tree (folders nest via parent id) and "
                + "every drawing with its current-revision standing — the approved revision label "
                + "when one is approved (a revision can be approved with a BLANK label, so trust "
                + "hasApprovedRevision, not the label), else the newest revision by file name, plus "
                + "unapproved and archived counts. Pass drawingId instead for that one drawing's "
                + "full revision history with the approval trail (who approved what, when, and what "
                + "was superseded).",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false),
                    ("drawingId", "string", "One drawing's full revision history instead of the register.", false)),
                AiToolKind.Read,
                DrawingReaders,
                async (context, input, ct) =>
                {
                    var drawingId = AiToolSchema.Text(input, "drawingId")?.Trim();
                    if (!string.IsNullOrWhiteSpace(drawingId))
                    {
                        var revisions = await context.Services
                            .GetRequiredService<IQueryHandler<ListRevisionsForDrawing, IReadOnlyList<DrawingRevision>>>()
                            .HandleAsync(new ListRevisionsForDrawing(drawingId), ct);
                        return Serialise(new
                        {
                            ok = true,
                            drawingId,
                            count = revisions.Count,
                            revisions = revisions.Select(revision => new
                            {
                                revision.DrawingRevisionId,
                                revision.RevisionLabel,
                                revision.FileName,
                                revision.IssuedByEmail,
                                revision.ReceivedAt,
                                revision.SupersededAt,
                                approvalStatus = revision.ApprovalStatus.ToString(),
                                revision.ApprovedByEmail,
                                revision.ApprovedAt
                            })
                        });
                    }

                    var projectId = ProjectId(context, input);
                    if (string.IsNullOrWhiteSpace(projectId))
                        return Fail("Say which project: pass projectId (list_projects returns ids).");

                    var drawings = await context.Services
                        .GetRequiredService<IQueryHandler<ListDrawingsForProject, IReadOnlyList<Drawing>>>()
                        .HandleAsync(new ListDrawingsForProject(projectId), ct);
                    var folders = await context.Services
                        .GetRequiredService<IQueryHandler<ListDrawingFoldersForProject, IReadOnlyList<DrawingFolder>>>()
                        .HandleAsync(new ListDrawingFoldersForProject(projectId), ct);

                    return Serialise(new
                    {
                        ok = true,
                        projectId,
                        folders = folders.Select(folder => new
                        {
                            folder.DrawingFolderId,
                            folder.Name,
                            parentFolderId = folder.ParentDrawingFolderId
                        }),
                        count = drawings.Count,
                        drawings = drawings.Select(drawing => new
                        {
                            drawing.DrawingId,
                            drawing.DrawingCode,
                            drawing.Title,
                            folderId = drawing.DrawingFolderId,
                            hasApprovedRevision = drawing.HasApprovedRevision,
                            currentApprovedRevisionLabel = drawing.CurrentApprovedRevisionLabel,
                            latestFileName = drawing.LatestFileName,
                            unapprovedCount = drawing.UnapprovedCount,
                            archivedCount = drawing.ArchivedCount
                        })
                    });
                }),

            new(
                "get_package_reconciliation",
                "A project's package reconciliation in one answer: the saved packages (each a named "
                + "group of work orders and sales slices, with its lock state — locked packages "
                + "freeze their figures at lock) and the per-package report rows: sales value, "
                + "claimed to date, target cost, WO committed, invoiced to date, drawdown (budget "
                + "left to commit), margin (live forecast buying gain) and the profit/loss realised "
                + "on lock. The save_reconciliation_package and set_reconciliation_package_lock "
                + "actions act on exactly this — read it first.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false)),
                AiToolKind.Read,
                ReconciliationReaders,
                async (context, input, ct) =>
                {
                    var projectId = ProjectId(context, input);
                    if (string.IsNullOrWhiteSpace(projectId))
                        return Fail("Say which project: pass projectId (list_projects returns ids).");

                    var packages = await context.Services
                        .GetRequiredService<IQueryHandler<ListReconciliationPackagesForProject, IReadOnlyList<ReconciliationPackage>>>()
                        .HandleAsync(new ListReconciliationPackagesForProject(projectId), ct);
                    var rows = await context.Services
                        .GetRequiredService<IQueryHandler<ListPackageReconciliation, IReadOnlyList<PackageReconciliationRow>>>()
                        .HandleAsync(new ListPackageReconciliation(projectId), ct);

                    return Serialise(new
                    {
                        ok = true,
                        projectId,
                        packages = packages.Select(package => new
                        {
                            package.ReconciliationPackageId,
                            package.Name,
                            package.WorkOrderIds,
                            salesLines = package.SalesLines,
                            costLines = package.DirectCosts,
                            package.IsLocked,
                            package.LockedAt
                        }),
                        reconciliation = rows
                    });
                })
        };
    }
}
