using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Api.Features.Registers;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.UsefulInformation;
using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Contracts.Rates;
using Jewel.JPMS.Contracts.Registers;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.UsefulInformation;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The company registers, readable (2026-08-31): the lead pipeline, the rate library, tender
/// enquiries, the client/architect/worker directories, the Monday-replacement company registers,
/// the sign-in directory, the cross-project RFI register and per-project Useful Information.
/// Each tool wraps the SAME query handler its HTTP endpoint composes and mirrors that endpoint's
/// role gate exactly.
/// </summary>
internal static class AiRegisterTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    /// <summary>Mirror of ListDirectoryUsersEndpoint's gate: AdminGate (Admin, FinanceDirector)
    /// unioned with its AllowedToReadStaffList — AdminGate adds nothing the list lacks, so the
    /// union is exactly this set.</summary>
    private static readonly RoleSet DirectoryReaders = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    public static IReadOnlyList<AiTool> Build()
    {
        return new List<AiTool>
        {
            // list_leads moved to AiSalesTools with the Sales rebuild (2026-09-06).
            new(
                "list_rates",
                "The company rate library — every priced rate: trade, description, supplier, "
                + "unit, the rate itself and when it was last priced. Staleness is judged from "
                + "lastPricedAt (a rate not re-priced for months is suspect for tendering). The "
                + "add_rate and revise_rate actions act on this list — read it first so a revision "
                + "targets the right RateId and an addition does not duplicate an existing rate.",
                AiToolSchema.Empty(),
                AiToolKind.Read,
                // Mirror of ListRatesInLibraryEndpoint.RolesThatMayReadRates (= AllInternal).
                JpmsRoleSets.AllInternal,
                async (context, _, ct) =>
                {
                    var rates = await context.Services
                        .GetRequiredService<IQueryHandler<ListRatesInLibrary, IReadOnlyList<Rate>>>()
                        .HandleAsync(new ListRatesInLibrary(), ct);
                    return Serialise(new
                    {
                        ok = true,
                        count = rates.Count,
                        rates = rates.Select(rate => new
                        {
                            rate.RateId,
                            rate.Trade,
                            rate.Description,
                            supplier = rate.SupplierName,
                            rate.Unit,
                            rate = rate.Value,
                            lastPricedAt = rate.LastPricedAt
                        })
                    });
                }),

            new(
                "list_clients",
                "The global client directory — every client account with its primary contact name "
                + "and email. One client can own many projects; a project or request deals either "
                + "with the client directly or with an architect acting for them "
                + "(list_architects). Use it to resolve a ClientId or find who to address.",
                AiToolSchema.Empty(),
                AiToolKind.Read,
                // Mirror of ListClientsEndpoint.RolesThatMayReadClients (= AllInternal).
                JpmsRoleSets.AllInternal,
                async (context, _, ct) =>
                {
                    var clients = await context.Services
                        .GetRequiredService<IQueryHandler<ListClients, IReadOnlyList<Client>>>()
                        .HandleAsync(new ListClients(), ct);
                    return Serialise(new
                    {
                        ok = true,
                        count = clients.Count,
                        clients = clients.Select(client => new
                        {
                            client.ClientId,
                            client.Name,
                            client.PrimaryContactName,
                            client.PrimaryContactEmail,
                            client.CreatedAt
                        })
                    });
                }),

            new(
                "list_architects",
                "The global architect-practice directory — every practice with its contact name "
                + "and email (where RFIs and request documents are addressed when an architect is "
                + "the selected party). Managed separately from clients: an architect typically "
                + "acts for a client on a project. Use it to resolve an ArchitectId.",
                AiToolSchema.Empty(),
                AiToolKind.Read,
                // Mirror of ListArchitectsEndpoint.RolesThatMayReadArchitects (= AllInternal).
                JpmsRoleSets.AllInternal,
                async (context, _, ct) =>
                {
                    var architects = await context.Services
                        .GetRequiredService<IQueryHandler<ListArchitects, IReadOnlyList<Architect>>>()
                        .HandleAsync(new ListArchitects(), ct);
                    return Serialise(new
                    {
                        ok = true,
                        count = architects.Count,
                        architects = architects.Select(architect => new
                        {
                            architect.ArchitectId,
                            architect.Name,
                            architect.ContactName,
                            architect.ContactEmail,
                            architect.CreatedAt
                        })
                    });
                }),

            new(
                "list_workers",
                "The worker registry — every site operative (day-rate subcontractor labour) with "
                + "their hourly rate (agreed day rate ÷ 8), active flag, linked subcontractor and "
                + "contact details. Rates are commercial-team data and NEVER reach site-capture "
                + "surfaces; a rate change applies to FUTURE timesheet approvals only — approved "
                + "historic timesheets keep the rate snapshotted at approval. Read this before any "
                + "worker or rate action.",
                AiToolSchema.Empty(),
                AiToolKind.Read,
                LabourRoleSets.ManageWorkers,
                async (context, _, ct) =>
                {
                    var workers = await context.Services
                        .GetRequiredService<IQueryHandler<ListWorkers, IReadOnlyList<Worker>>>()
                        .HandleAsync(new ListWorkers(), ct);
                    return Serialise(new
                    {
                        ok = true,
                        count = workers.Count,
                        workers = workers.Select(worker => new
                        {
                            worker.WorkerId,
                            worker.Name,
                            worker.SubcontractorId,
                            worker.HourlyRate,
                            worker.IsActive,
                            worker.ContactEmail,
                            worker.ContactPhone
                        })
                    });
                }),

            new(
                "list_company_registers",
                "The company registers (the Monday-board replacement), active rows first: "
                + "insurances, subscriptions, vans and trade accounts, each with counterparty, "
                + "reference, owner, cost, billing cycle and its key dates. Field meaning shifts "
                + "with the kind — Insurance: keyDate=renewal; Subscription: keyDate=next renewal, "
                + "secondaryDate=cancellation notice by; Van: counterparty=driver, "
                + "reference=registration, keyDate=MOT due, secondaryDate=tax due; TradeAccount: "
                + "keyDate=review date. Call this for anything about renewals, cover or the fleet.",
                AiToolSchema.Empty(),
                AiToolKind.Read,
                RegisterRoleSets.ManageRegisters,
                async (context, _, ct) =>
                {
                    var items = await context.Services
                        .GetRequiredService<IQueryHandler<ListRegisterItems, IReadOnlyList<RegisterItem>>>()
                        .HandleAsync(new ListRegisterItems(), ct);
                    return Serialise(new
                    {
                        ok = true,
                        count = items.Count,
                        items = items.Select(item => new
                        {
                            item.RegisterItemId,
                            kind = item.Kind.ToString(),
                            item.Name,
                            item.Counterparty,
                            item.Reference,
                            item.OwnerEmail,
                            item.Cost,
                            item.BillingCycle,
                            item.KeyDate,
                            item.SecondaryDate,
                            item.Notes,
                            item.IsActive
                        })
                    });
                }),

            new(
                "list_portal_users",
                "The portal sign-in directory — every active internal user with the roles they "
                + "hold (revoked users are excluded). Use it to answer who has access, who holds a "
                + "role, or to resolve a colleague's email. Read-only: managing users stays with "
                + "the administrators in the portal.",
                AiToolSchema.Empty(),
                AiToolKind.Read,
                DirectoryReaders,
                async (context, _, ct) =>
                {
                    var users = await context.Services
                        .GetRequiredService<IQueryHandler<ListDirectoryUsers, IReadOnlyList<DirectoryUser>>>()
                        .HandleAsync(new ListDirectoryUsers(), ct);
                    return Serialise(new
                    {
                        ok = true,
                        count = users.Count,
                        users = users.Select(user => new
                        {
                            user.Email,
                            user.DisplayName,
                            roles = user.Roles.Select(role => role.ToString())
                        })
                    });
                }),

            new(
                "list_rfis_across_projects",
                "The cross-project RFI register: every RFI on every live project, newest first, "
                + "with status (NeedsAction = ball with us, Open = awaiting the architect, "
                + "NeedsVariation, Closed), issued date, response-due date, days outstanding and "
                + "the critical-path flag. This is the portfolio-wide view — list_requests is "
                + "per-project and covers the other request kinds; get_request_context reads one "
                + "in full.",
                AiToolSchema.Empty(),
                AiToolKind.Read,
                RfiDashboardRoles.AllowedToViewDashboard,
                async (context, _, ct) =>
                {
                    var rfis = await context.Services
                        .GetRequiredService<IQueryHandler<ListRfisAcrossProjects, IReadOnlyList<Request>>>()
                        .HandleAsync(new ListRfisAcrossProjects(), ct);
                    return Serialise(new
                    {
                        ok = true,
                        count = rfis.Count,
                        rfis = rfis.Select(rfi => new
                        {
                            rfi.RequestId,
                            rfi.ProjectId,
                            number = rfi.DisplayNumber,
                            rfi.Reference,
                            rfi.Title,
                            status = rfi.Status.ToString(),
                            rfi.IssuedAt,
                            rfi.ResponseDue,
                            rfi.RespondedAt,
                            rfi.ClosedAt,
                            daysOutstanding = rfi.DaysOutstanding,
                            criticalPath = rfi.CriticalPath,
                            rfi.ImpliesVariation
                        })
                    });
                }),

            new(
                "list_useful_information",
                "A project's Useful Information notes — titled free-text reference the office "
                + "keeps against the project: door codes, key-safe locations, skip access, site "
                + "quirks. STRICTLY INTERNAL by design: never repeat these in anything client-, "
                + "architect- or subcontractor-facing. Reference material only — anything that "
                + "needs doing lives on the To-do tab, not here.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view; pass it otherwise (list_projects returns ids).", false)),
                AiToolKind.Read,
                UsefulInformationRoles.AllowedToRead,
                async (context, input, ct) =>
                {
                    var projectId = AiToolSchema.Text(input, "projectId") ?? context.Scope?.ProjectId;
                    if (string.IsNullOrWhiteSpace(projectId))
                        return Fail("Say which project: pass projectId (list_projects returns ids).");

                    var notes = await context.Services
                        .GetRequiredService<IQueryHandler<ListUsefulInformationForProject, IReadOnlyList<UsefulInformationNote>>>()
                        .HandleAsync(new ListUsefulInformationForProject(projectId), ct);
                    return Serialise(new
                    {
                        ok = true,
                        projectId,
                        count = notes.Count,
                        notes = notes.Select(note => new
                        {
                            note.UsefulInformationNoteId,
                            note.Title,
                            note.Body,
                            note.CreatedByEmail,
                            note.CreatedAt,
                            note.UpdatedByEmail,
                            note.UpdatedAt
                        })
                    });
                })
        };
    }
}
