using Jewel.JPMS.Api.Features.TenderEnquiries.Documents;
using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Queries;

/// <summary>Renders the PQQ response on demand from SQL — the same builder + renderer the email
/// attach uses, so the downloaded file is byte-for-byte the one the architect receives.</summary>
public sealed class GetTenderEnquiryDocumentHandler : IQueryHandler<GetTenderEnquiryDocument, TenderEnquiryDocumentFile?>
{
    private const string PdfContentType = "application/pdf";

    private readonly JpmsContext context;

    public GetTenderEnquiryDocumentHandler(JpmsContext context) { this.context = context; }

    public async Task<TenderEnquiryDocumentFile?> HandleAsync(GetTenderEnquiryDocument query, CancellationToken cancellationToken)
    {
        var model = await TenderEnquiryDocumentBuilder.BuildAsync(context, query.TenderEnquiryId, cancellationToken);
        if (model is null) return null;
        return new TenderEnquiryDocumentFile(model.FileName, PdfContentType, TenderEnquiryDocumentRenderer.Render(model));
    }
}
