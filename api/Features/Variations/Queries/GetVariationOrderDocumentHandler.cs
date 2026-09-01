using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Variations.Documents;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

/// <summary>
/// Renders the variation order document on demand from SQL — the exact same builder + renderer the
/// email attach uses, so the downloaded file is byte-for-byte the one a recipient receives.
/// </summary>
public sealed class GetVariationOrderDocumentHandler : IQueryHandler<GetVariationOrderDocument, VariationDocumentFile?>
{
    private readonly JpmsContext context;

    public GetVariationOrderDocumentHandler(JpmsContext context)
    { this.context = context; }

    public async Task<VariationDocumentFile?> HandleAsync(GetVariationOrderDocument query, CancellationToken cancellationToken)
    {
        var model = await VariationDocumentBuilder.BuildAsync(context, query.VariationOrderId, cancellationToken);
        if (model is null) return null;

        var pdf = VariationDocumentRenderer.Render(model);
        return new VariationDocumentFile(model.FileName, "application/pdf", pdf);
    }
}
