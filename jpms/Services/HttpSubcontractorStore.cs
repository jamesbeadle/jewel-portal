using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Subcontractors;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpSubcontractorStore : ISubcontractorStore
{
    private readonly SubcontractorsReadModel readModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    // Compliance documents per subcontractor, cached so render-time reads never block on async
    // (which deadlocks on WebAssembly). Saving a document invalidates its subcontractor.
    private readonly AsyncQueryCache<string, IReadOnlyList<ComplianceDocument>> compliance;

    public HttpSubcontractorStore(SubcontractorsReadModel readModel, IQueryClient queries, ICommandSender commands)
    {
        this.readModel = readModel;
        this.queries = queries;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
        compliance = new((id, ct) => queries.AskAsync(new ListComplianceDocumentsForSubcontractor(id), ct), () => OnChange?.Invoke());
    }

    public event Action? OnChange;

    public IReadOnlyList<Subcontractor> All()
    {
        if (readModel.Current is null) _ = readModel.RefreshAsync(CancellationToken.None);
        return readModel.Current ?? Array.Empty<Subcontractor>();
    }

    public Subcontractor? Find(string subcontractorId) =>
        All().FirstOrDefault(sub => string.Equals(sub.SubcontractorId, subcontractorId, StringComparison.OrdinalIgnoreCase));

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
        await commands.SendAsync(new AddSubcontractorToDirectory(sub.CompanyName, sub.PrimaryTrade, sub.ContactName, sub.ContactEmail, sub.ContactPhone, sub.CisStatus,
            sub.Category, sub.MobileNumber, sub.Town, sub.County, sub.Website), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }

    private async Task UpdateAsync(Subcontractor sub)
    {
        await commands.SendAsync(new UpdateSubcontractor(sub.SubcontractorId, sub.CompanyName, sub.PrimaryTrade, sub.ContactName, sub.ContactEmail, sub.ContactPhone, sub.CisStatus), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }
}
