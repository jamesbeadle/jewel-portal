using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ValuationInvoices;

public sealed record GetProjectValuationInvoiceSummary(string ProjectId) : IQuery<ProjectValuationInvoiceSummary>;
