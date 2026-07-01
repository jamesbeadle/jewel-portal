using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IBoqStore
{
    IReadOnlyList<BoqLineItem> LinesFor(string projectId);

    BoqLineItem Upsert(BoqLineItem line);

    bool Remove(string boqLineItemId);

    decimal TotalFor(string projectId);

    Task<BoqSignOff?> SignOffForAsync(string projectId);
    BoqSignOff RecordSignOff(BoqSignOff signOff);

    event Action? OnChange;
}
