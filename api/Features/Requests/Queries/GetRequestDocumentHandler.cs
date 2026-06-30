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
    private readonly RequestEmailReader emails;

    public GetRequestDocumentHandler(JpmsContext context, RequestEmailReader emails)
    { this.context = context; this.emails = emails; }

    public async Task<RequestDocumentFile?> HandleAsync(GetRequestDocument query, CancellationToken cancellationToken)
    {
        var tagged = await emails.ForRequestAsync(query.RequestId, cancellationToken);
        var model = await RequestDocumentBuilder.BuildAsync(context, query.RequestId, tagged, cancellationToken);
        if (model is null) return null;

        var pdf = RequestDocumentRenderer.Render(model);
        return new RequestDocumentFile(model.FileName, "application/pdf", pdf);
    }
}
