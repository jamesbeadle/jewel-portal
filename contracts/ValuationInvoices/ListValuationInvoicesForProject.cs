using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ValuationInvoices;

public sealed record ListValuationInvoicesForProject(string ProjectId) : IQuery<IReadOnlyList<ValuationInvoice>>;
