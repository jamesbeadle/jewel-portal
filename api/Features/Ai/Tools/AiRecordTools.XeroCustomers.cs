using Jewel.JPMS.Contracts.Xero;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The Xero contact list as connector reads: the same ListXeroSuppliers query the directory's
/// Import-from-Xero modal and Project settings use. list_xero_customers (2026-09-10) filters it to
/// the contacts a sales invoice can be raised on — the model reads the names and proposes the
/// match for a project the way a person does ("Ravenswood Ave" is "64 Ravenswood Avenue"), then,
/// with the user's yes, set_project_xero_contact stores it. list_xero_suppliers (2026-09-15) is the
/// modal's own view — every contact with its imported / linked / name-match stamps — so the model
/// can find the ContactID import_xero_supplier and link_directory_record_to_xero_contact need
/// instead of asking the user to paste it from Xero's URL. Nothing here writes.
/// </summary>
internal static partial class AiRecordTools
{
    private static readonly RoleSet XeroCustomerReaders =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    // Mirrors ListXeroSuppliersEndpoint's gate (the directory managers who may import).
    private static readonly RoleSet XeroSupplierReaders = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);

    private static IEnumerable<AiTool> XeroCustomerTools() => new AiTool[]
    {
        new(
            "list_xero_customers",
            "The contacts Xero holds that a sales invoice can be raised on — every active contact "
            + "flagged as a customer, plus contacts Xero has not flagged either way yet (a brand-new "
            + "contact carries neither flag); supplier-only contacts are left out. Each row is the "
            + "Xero contactId and the name exactly as Xero holds it, with town and postcode where "
            + "Xero has them. This is what Project settings' Xero-contact picker reads. Use it to "
            + "find the customer a project's invoices go to: match on the address or client the "
            + "project names — spelling and abbreviations differ (\"Ravenswood Ave\" IS \"64 "
            + "Ravenswood Avenue\") — propose the match to the user, and on their yes call "
            + "set_project_xero_contact with the contactId. Pass search to narrow by name.",
            AiToolSchema.Object(
                ("search", "string", "Optional text matched against contact names, towns and postcodes (case-insensitive).", false),
                ("includeSuppliers", "boolean", "true also returns supplier-only contacts. Default false.", false)),
            AiToolKind.Read,
            XeroCustomerReaders,
            async (context, input, ct) =>
            {
                var snapshot = await context.Services
                    .GetRequiredService<IQueryHandler<ListXeroSuppliers, XeroSuppliersSnapshot>>()
                    .HandleAsync(new ListXeroSuppliers(), ct);

                if (!snapshot.IsConfigured) return Fail("Xero is not connected on this portal.");
                if (snapshot.Error is not null) return Fail($"Xero could not be read: {snapshot.Error}");

                var search = AiToolSchema.Text(input, "search")?.Trim();
                var includeSuppliers = AiToolSchema.Flag(input, "includeSuppliers") ?? false;

                var rows = snapshot.Suppliers
                    .Where(contact => includeSuppliers || contact.IsCustomer || !contact.IsSupplier)
                    .Where(contact => string.IsNullOrEmpty(search)
                        || contact.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || contact.Town.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || contact.Postcode.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(contact => contact.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(contact => new
                    {
                        contactId = contact.ContactId,
                        name = contact.Name,
                        town = contact.Town,
                        postcode = contact.Postcode,
                        isCustomer = contact.IsCustomer,
                        isSupplier = contact.IsSupplier
                    })
                    .ToList();

                return Serialise(new
                {
                    ok = true,
                    fetchedAtUtc = snapshot.FetchedAtUtc,
                    truncated = snapshot.Truncated,
                    count = rows.Count,
                    contacts = rows,
                    note = "Match the project's client/address to a name here by reading, not by exact text; "
                           + "show the user the proposed contact (name and town) and take their yes, then "
                           + "set_project_xero_contact(projectId, xeroContactId). The portal re-reads the "
                           + "contact from Xero and stores Xero's own name."
                });
            }),
        new(
            "list_xero_suppliers",
            "The contacts Xero holds, as the Directory page's Import-from-Xero modal lists them: every "
            + "active contact (not only those Xero flags as a supplier — that flag only turns on after "
            + "a first bill, so a contact created moments ago carries neither flag), each with the Xero "
            + "contactId, the name exactly as Xero holds it, email, phone, town and postcode, and the "
            + "directory's stamps: alreadyImported / linkedSubcontractorId when a directory record is "
            + "already linked to it, and matchingSubcontractorId / matchingSubcontractorName when an "
            + "UNLINKED directory record's name matches. This is where the xeroContactId for "
            + "import_xero_supplier and link_directory_record_to_xero_contact comes from — never ask "
            + "the user to copy it out of Xero. Read it as: alreadyImported → the record exists, open "
            + "it with search_directory; a matching record → link_directory_record_to_xero_contact, "
            + "not a second record; neither → import_xero_supplier. Pass search to narrow by name "
            + "(spelling drifts — \"On the Level\" may be \"On The Level (Wet Rooms) Ltd\", so search "
            + "a distinctive word, not the whole name).",
            AiToolSchema.Object(
                ("search", "string", "Optional text matched against contact names, emails, towns and postcodes (case-insensitive).", false),
                ("includeCustomers", "boolean", "true also returns customer-only contacts. Default false.", false)),
            AiToolKind.Read,
            XeroSupplierReaders,
            async (context, input, ct) =>
            {
                var snapshot = await context.Services
                    .GetRequiredService<IQueryHandler<ListXeroSuppliers, XeroSuppliersSnapshot>>()
                    .HandleAsync(new ListXeroSuppliers(), ct);

                if (!snapshot.IsConfigured) return Fail("Xero is not connected on this portal.");
                if (snapshot.Error is not null) return Fail($"Xero could not be read: {snapshot.Error}");

                var search = AiToolSchema.Text(input, "search")?.Trim();
                var includeCustomers = AiToolSchema.Flag(input, "includeCustomers") ?? false;

                var rows = snapshot.Suppliers
                    .Where(contact => includeCustomers || contact.IsSupplier || !contact.IsCustomer)
                    .Where(contact => string.IsNullOrEmpty(search)
                        || contact.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || contact.EmailAddress.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || contact.Town.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || contact.Postcode.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(contact => contact.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(contact => new
                    {
                        contactId = contact.ContactId,
                        name = contact.Name,
                        email = contact.EmailAddress,
                        phone = contact.Phone,
                        town = contact.Town,
                        postcode = contact.Postcode,
                        isSupplier = contact.IsSupplier,
                        isCustomer = contact.IsCustomer,
                        alreadyImported = contact.AlreadyImported,
                        linkedSubcontractorId = contact.LinkedSubcontractorId,
                        matchingSubcontractorId = contact.MatchingSubcontractorId,
                        matchingSubcontractorName = string.IsNullOrEmpty(contact.MatchingSubcontractorName) ? null : contact.MatchingSubcontractorName
                    })
                    .ToList();

                return Serialise(new
                {
                    ok = true,
                    fetchedAtUtc = snapshot.FetchedAtUtc,
                    truncated = snapshot.Truncated,
                    count = rows.Count,
                    contacts = rows,
                    note = "A contact absent here is not in Xero (or the read was truncated — check truncated). "
                           + "Then add_subcontractor_to_directory creates the record directly: it needs "
                           + "tradeIds from list_trades, and a Supplier-category record can be linked to "
                           + "Xero later once the contact exists there."
                });
            })
    };
}
