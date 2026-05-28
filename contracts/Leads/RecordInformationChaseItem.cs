using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Leads;

public sealed record RecordInformationChaseItem(
    string LeadId,
    string Kind,
    string Description,
    bool IsReceived) : ICommand<InfoChaseItem>;
