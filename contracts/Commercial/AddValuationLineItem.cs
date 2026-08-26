using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

public sealed record AddValuationLineItem(
    string ProjectId,
    ValuationElementType ElementType,
    string SectionCode,
    string SectionName,
    string VariationRef,
    string VariationTitle,
    ValuationLineType LineType,
    string CostCode,
    string Description,
    string Unit,
    decimal Quantity,
    decimal Rate,
    string Comments,
    int DisplayOrder,
    // Line-level client schedule-of-works ref ("1.03"); trailing default keeps older callers stable.
    string ClientReference = "") : ICommand<ValuationLineItem>;
