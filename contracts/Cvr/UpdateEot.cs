using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Cvr;

public sealed record UpdateEot(
    string EotId,
    string Reason,
    int DaysGranted,
    decimal CommercialRecovery) : ICommand<Eot>;
