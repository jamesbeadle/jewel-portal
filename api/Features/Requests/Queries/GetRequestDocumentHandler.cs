using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Requests.Documents;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

/// <summary>
/// Renders the request document on demand from SQL. Uses the exact same builder + renderer the worker
/// uses to email the PDF, so the downloaded file is byte-for-byte the one the recipients receive.
/// </summary>
public sealed class GetRequestDocumentHandler : IQueryHandler<GetRequestDocument, RequestDocumentFile?>
{
    private readonly JpmsContext context;

    public GetRequestDocumentHandler(JpmsContext context) { this.context = context; }

    public async Task<RequestDocumentFile?> HandleAsync(GetRequestDocument query, CancellationToken cancellationToken)
    {
        var model = await RequestDocumentBuilder.BuildAsync(context, query.RequestId, cancellationToken);
        if (model is null) return null;

        var pdf = RequestDocumentRenderer.Render(model);
        return new RequestDocumentFile(model.FileName, "application/pdf", pdf);
    }
}
