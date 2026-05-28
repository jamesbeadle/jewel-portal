using Jewel.JPMS.Contracts.Rates;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Rates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpRateLibrary : IRateLibrary
{
    private readonly RateLibraryReadModel readModel;
    private readonly ICommandSender commands;

    public HttpRateLibrary(RateLibraryReadModel readModel, ICommandSender commands)
    {
        this.readModel = readModel;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<Rate> All()
    {
        if (readModel.Current is null) _ = readModel.RefreshAsync(CancellationToken.None);
        return readModel.Current ?? Array.Empty<Rate>();
    }

    public Rate? Find(string rateId) =>
        All().FirstOrDefault(rate => string.Equals(rate.RateId, rateId, StringComparison.OrdinalIgnoreCase));

    public Rate Upsert(Rate rate)
    {
        if (Find(rate.RateId) is null) _ = AddAsync(rate);
        else _ = ReviseAsync(rate);
        return rate;
    }

    public IReadOnlyList<Rate> Stale(int dayThreshold) =>
        All().Where(rate => rate.IsStale(dayThreshold)).ToList().AsReadOnly();

    private async Task AddAsync(Rate rate)
    {
        await commands.SendAsync(new AddRate(rate.Trade, rate.Description, rate.Unit, rate.Value, rate.SupplierName), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }

    private async Task ReviseAsync(Rate rate)
    {
        await commands.SendAsync(new ReviseRate(rate.RateId, rate.Trade, rate.Description, rate.Unit, rate.Value, rate.SupplierName), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }
}
