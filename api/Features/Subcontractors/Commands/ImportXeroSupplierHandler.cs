using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>
/// Copies one Xero supplier into the company directory: a new record (category Supplier, no trades
/// — those are curated by hand afterwards), a SubcontractorXeroLink row marking the record linked
/// to Xero, and one company contact row per Xero contact person. Importing never merges into an
/// existing record — duplicates are resolved afterwards through ConsolidateDirectoryRecords, so
/// there is one consistent flow for all duplicates.
/// </summary>
public sealed class ImportXeroSupplierHandler : ICommandHandler<ImportXeroSupplier, Subcontractor>
{
    private readonly JpmsContext context;
    private readonly IXeroClient xero;
    private readonly AuditActor actor;

    public ImportXeroSupplierHandler(JpmsContext context, IXeroClient xero, AuditActor actor)
    {
        this.context = context;
        this.xero = xero;
        this.actor = actor;
    }

    public async Task<Subcontractor> HandleAsync(ImportXeroSupplier command, CancellationToken cancellationToken)
    {
        // The unique index backs this up, but checking first gives the caller a message rather
        // than a database error when the same supplier is imported twice (e.g. two open tabs).
        var alreadyLinked = await context.SubcontractorXeroLinks
            .AnyAsync(link => link.XeroContactId == command.XeroContactId, cancellationToken);
        if (alreadyLinked)
            throw new InvalidOperationException("That Xero supplier has already been imported into the directory.");

        var supplier = await FindSupplierAsync(command.XeroContactId, cancellationToken);

        var entity = new SubcontractorEntity
        {
            SubcontractorId = SubcontractorIdentifierFactory.NextSubcontractorId(),
            CompanyName = supplier.Name,
            ContactName = supplier.ContactPersons.Count > 0 ? supplier.ContactPersons[0].Name : "",
            ContactEmail = supplier.EmailAddress,
            ContactPhone = supplier.Phone,
            CisStatus = "",
            OnboardedAt = DateTimeOffset.UtcNow,
            Category = (int)DirectoryCategory.Supplier,
            MobileNumber = supplier.Mobile,
            Town = supplier.Town,
            County = supplier.County
        };
        context.Subcontractors.Add(entity);

        context.SubcontractorXeroLinks.Add(new SubcontractorXeroLinkEntity
        {
            SubcontractorXeroLinkId = SubcontractorIdentifierFactory.NextSubcontractorXeroLinkId(),
            SubcontractorId = entity.SubcontractorId,
            XeroContactId = supplier.ContactId,
            XeroContactName = supplier.Name,
            ImportedAt = DateTimeOffset.UtcNow,
            ImportedByEmail = actor.Email
        });

        // Xero's additional contact persons come across as company contacts, so nothing Xero
        // holds about who to talk to is lost. The first person also seeds the primary line above.
        foreach (var person in supplier.ContactPersons)
        {
            context.CompanyContacts.Add(new CompanyContactEntity
            {
                CompanyContactId = SubcontractorIdentifierFactory.NextCompanyContactId(),
                SubcontractorId = entity.SubcontractorId,
                Name = person.Name,
                Purpose = "",
                Email = person.EmailAddress,
                Phone = "",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel(Array.Empty<Trade>(), xeroLinked: true);
    }

    private async Task<XeroSupplier> FindSupplierAsync(string xeroContactId, CancellationToken cancellationToken)
    {
        // The cached snapshot is normally fresh (the import modal just listed it); fall back to a
        // forced read once in case the cache predates a supplier created moments ago in Xero.
        var snapshot = await xero.GetSuppliersAsync(force: false, cancellationToken);
        var supplier = Match(snapshot, xeroContactId);
        if (supplier is null && snapshot.IsConfigured && snapshot.Error is null)
        {
            snapshot = await xero.GetSuppliersAsync(force: true, cancellationToken);
            supplier = Match(snapshot, xeroContactId);
        }

        if (!snapshot.IsConfigured)
            throw new InvalidOperationException("Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings.");
        if (snapshot.Error is not null)
            throw new InvalidOperationException(snapshot.Error);
        return supplier
            ?? throw new InvalidOperationException("That supplier wasn't found in Xero. Refresh the list and try again.");
    }

    private static XeroSupplier? Match(XeroSuppliersSnapshot snapshot, string xeroContactId) =>
        snapshot.Suppliers.FirstOrDefault(supplier =>
            string.Equals(supplier.ContactId, xeroContactId, StringComparison.OrdinalIgnoreCase));
}
