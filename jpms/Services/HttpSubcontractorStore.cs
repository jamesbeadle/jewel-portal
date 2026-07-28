using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Subcontractors;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpSubcontractorStore : ISubcontractorStore
{
    private readonly SubcontractorsReadModel readModel;
    private readonly TradesReadModel tradesReadModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    // Compliance documents per subcontractor, cached so render-time reads never block on async
    // (which deadlocks on WebAssembly). Saving a document invalidates its subcontractor.
    private readonly AsyncQueryCache<string, IReadOnlyList<ComplianceDocument>> compliance;

    // Company contacts per record, cached for the same render-time reason. Upserting or removing
    // a contact invalidates its record.
    private readonly AsyncQueryCache<string, IReadOnlyList<CompanyContact>> contacts;

    public HttpSubcontractorStore(SubcontractorsReadModel readModel, TradesReadModel tradesReadModel, IQueryClient queries, ICommandSender commands)
    {
        this.readModel = readModel;
        this.tradesReadModel = tradesReadModel;
        this.queries = queries;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
        tradesReadModel.OnChanged += () => OnChange?.Invoke();
        compliance = new((id, ct) => queries.AskAsync(new ListComplianceDocumentsForSubcontractor(id), ct), () => OnChange?.Invoke());
        contacts = new((id, ct) => queries.AskAsync(new ListCompanyContacts(id), ct), () => OnChange?.Invoke());
    }

    public event Action? OnChange;

    public bool IsLoaded => readModel.Current is not null;

    public IReadOnlyList<Subcontractor> All()
    {
        if (readModel.Current is null) _ = readModel.RefreshAsync(CancellationToken.None);
        return readModel.Current ?? Array.Empty<Subcontractor>();
    }

    public Subcontractor? Find(string subcontractorId) =>
        All().FirstOrDefault(sub => string.Equals(sub.SubcontractorId, subcontractorId, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<Trade> Trades()
    {
        if (tradesReadModel.Current is null) _ = tradesReadModel.RefreshAsync(CancellationToken.None);
        return tradesReadModel.Current ?? Array.Empty<Trade>();
    }

    public bool TradesLoaded => tradesReadModel.Current is not null;

    public async Task<Trade> AddTradeAsync(string name)
    {
        var trade = await commands.SendAsync(new AddTrade(name), CancellationToken.None);
        await tradesReadModel.RefreshAsync(CancellationToken.None);
        return trade;
    }

    public async Task SetTradesAsync(string subcontractorId, IReadOnlyList<string> tradeIds)
    {
        var sub = Find(subcontractorId)
            ?? throw new InvalidOperationException($"Subcontractor {subcontractorId} not found.");
        await commands.SendAsync(new UpdateSubcontractor(
            sub.SubcontractorId, sub.CompanyName, tradeIds, sub.ContactName, sub.ContactEmail,
            sub.ContactPhone, sub.CisStatus), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }

    // Company name + contact details + payment terms, preserving the record's trades, CIS
    // status and every other field. Renaming to match the Xero supplier name is what lines
    // invoices up on the WO Allocation tab.
    public async Task UpdateDetailsAsync(string subcontractorId, string companyName,
        string contactName, string contactEmail, string contactPhone, int paymentTermsDays)
    {
        var sub = Find(subcontractorId)
            ?? throw new InvalidOperationException($"Subcontractor {subcontractorId} not found.");
        await commands.SendAsync(new UpdateSubcontractor(
            sub.SubcontractorId, companyName.Trim(), TradeIds(sub), contactName.Trim(),
            contactEmail.Trim(), contactPhone.Trim(), sub.CisStatus,
            PaymentTermsDays: paymentTermsDays), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }

    public Subcontractor Upsert(Subcontractor subcontractor)
    {
        if (string.IsNullOrEmpty(subcontractor.SubcontractorId))
            _ = AddAsync(subcontractor);
        else _ = UpdateAsync(subcontractor);
        return subcontractor;
    }

    public IReadOnlyList<ComplianceDocument> ComplianceFor(string subcontractorId) =>
        compliance.Get(subcontractorId, Array.Empty<ComplianceDocument>());

    public void SaveCompliance(ComplianceDocument document) => _ = SaveComplianceAsync(document);

    private async Task SaveComplianceAsync(ComplianceDocument document)
    {
        await commands.SendAsync(
            new UploadComplianceDocument(document.SubcontractorId, document.Kind, document.FileName, document.ExpiresAt),
            CancellationToken.None);
        compliance.Invalidate(document.SubcontractorId);
    }

    private async Task AddAsync(Subcontractor sub)
    {
        await commands.SendAsync(new AddSubcontractorToDirectory(sub.CompanyName, TradeIds(sub), sub.ContactName, sub.ContactEmail, sub.ContactPhone, sub.CisStatus,
            sub.Category, sub.MobileNumber, sub.Town, sub.County, sub.Website, sub.PaymentTermsDays), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }

    private async Task UpdateAsync(Subcontractor sub)
    {
        await commands.SendAsync(new UpdateSubcontractor(sub.SubcontractorId, sub.CompanyName, TradeIds(sub), sub.ContactName, sub.ContactEmail, sub.ContactPhone, sub.CisStatus,
            PaymentTermsDays: sub.PaymentTermsDays), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }

    // ---- Xero import + consolidation ----

    public Task<XeroSuppliersSnapshot> FetchXeroSuppliersAsync(bool force = false) =>
        queries.AskAsync(new ListXeroSuppliers(force), CancellationToken.None);

    public async Task<Subcontractor> ImportFromXeroAsync(string xeroContactId)
    {
        var imported = await commands.SendAsync(new ImportXeroSupplier(xeroContactId), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
        return imported;
    }

    public async Task<Subcontractor> ConsolidateAsync(ConsolidateDirectoryRecords command)
    {
        var master = await commands.SendAsync(command, CancellationToken.None);
        // The merged-away records' cached contacts AND compliance documents are stale — the
        // server moved their rows to the master.
        contacts.Invalidate(command.MasterSubcontractorId);
        compliance.Invalidate(command.MasterSubcontractorId);
        foreach (var mergedId in command.MergedSubcontractorIds)
        {
            contacts.Invalidate(mergedId);
            compliance.Invalidate(mergedId);
        }
        await readModel.RefreshAsync(CancellationToken.None);
        return master;
    }

    // ---- Company contacts ----

    public IReadOnlyList<CompanyContact> ContactsFor(string subcontractorId) =>
        contacts.Get(subcontractorId, Array.Empty<CompanyContact>());

    public bool ContactsLoadedFor(string subcontractorId) => contacts.Has(subcontractorId);

    public async Task UpsertContactAsync(UpsertCompanyContact command)
    {
        await commands.SendAsync(command, CancellationToken.None);
        contacts.Invalidate(command.SubcontractorId);
    }

    public async Task RemoveContactAsync(string subcontractorId, string companyContactId)
    {
        await commands.SendAsync(new RemoveCompanyContact(subcontractorId, companyContactId), CancellationToken.None);
        contacts.Invalidate(subcontractorId);
    }

    private static IReadOnlyList<string> TradeIds(Subcontractor sub) =>
        sub.Trades.Select(trade => trade.TradeId).ToList();
}
