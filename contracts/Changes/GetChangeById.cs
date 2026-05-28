using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Changes;

public sealed record GetChangeById(string ChangeRecordId) : IQuery<ChangeRecord?>;
