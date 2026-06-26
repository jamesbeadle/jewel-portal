namespace Jewel.JPMS.Services;

/// <summary>
/// Aggregates the change notifications of every data store into a single event.
///
/// The stores load their data lazily: the first time a component reads them they
/// kick off a background fetch and return an empty result, then raise
/// <c>OnChange</c> once the data arrives. A component only repaints in response to
/// that event if it happens to be subscribed — which is why, historically, a page
/// could render empty on first paint and only fill in after the user navigated
/// away and back (the second visit found the data already cached).
///
/// By having the shell layouts subscribe to this hub, ANY completed background
/// load repaints the current page, so every view self-heals without each page
/// having to wire up its own subscriptions.
/// </summary>
public sealed class StoreChangeHub : IDisposable
{
    private readonly List<Action> unsubscribers = new();

    public StoreChangeHub(
        IUserDirectory directory,
        IAccessRequestStore accessRequests,
        ILeadStore leads,
        ISubcontractorStore subcontractors,
        IRateLibrary rates,
        IHsRegister hs,
        IProcurementStore procurement,
        IBoqStore boq,
        IDrawingStore drawings,
        IMobilisationStore mobilisation,
        IRequestRegister requests,
        ISiteStore site,
        ICommercialStore commercial,
        ICommercialInputsStore commercialInputs,
        ICvrStore cvr,
        ICloseoutStore closeout)
    {
        Track(h => directory.OnChange += h, h => directory.OnChange -= h);
        Track(h => accessRequests.OnChange += h, h => accessRequests.OnChange -= h);
        Track(h => leads.OnChange += h, h => leads.OnChange -= h);
        Track(h => subcontractors.OnChange += h, h => subcontractors.OnChange -= h);
        Track(h => rates.OnChange += h, h => rates.OnChange -= h);
        Track(h => hs.OnChange += h, h => hs.OnChange -= h);
        Track(h => procurement.OnChange += h, h => procurement.OnChange -= h);
        Track(h => boq.OnChange += h, h => boq.OnChange -= h);
        Track(h => drawings.OnChange += h, h => drawings.OnChange -= h);
        Track(h => mobilisation.OnChange += h, h => mobilisation.OnChange -= h);
        Track(h => requests.OnChange += h, h => requests.OnChange -= h);
        Track(h => site.OnChange += h, h => site.OnChange -= h);
        Track(h => commercial.OnChange += h, h => commercial.OnChange -= h);
        Track(h => commercialInputs.OnChange += h, h => commercialInputs.OnChange -= h);
        Track(h => cvr.OnChange += h, h => cvr.OnChange -= h);
        Track(h => closeout.OnChange += h, h => closeout.OnChange -= h);
    }

    /// <summary>Raised whenever any underlying store reports a change.</summary>
    public event Action? OnAnyChange;

    private void Track(Action<Action> subscribe, Action<Action> unsubscribe)
    {
        void Relay() => OnAnyChange?.Invoke();
        subscribe(Relay);
        unsubscribers.Add(() => unsubscribe(Relay));
    }

    public void Dispose()
    {
        foreach (var unsubscribe in unsubscribers) unsubscribe();
        unsubscribers.Clear();
    }
}
