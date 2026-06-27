using Ganss.Xss;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

/// <summary>
/// Returns the full body + attachment list for one intake email, fetched live from Graph. The HTML
/// body is sanitised here before it leaves the server, so the Blazor client can render it directly.
/// If Graph is unavailable or the email has no Graph handle, we fall back to the stored preview so
/// the triager always sees something rather than an error.
/// </summary>
public sealed class GetIntakeEmailDetailHandler : IQueryHandler<GetIntakeEmailDetail, IntakeEmailDetail>
{
    private readonly JpmsContext context;
    private readonly IIntakeMessageReader reader;
    public GetIntakeEmailDetailHandler(JpmsContext context, IIntakeMessageReader reader)
    {
        this.context = context;
        this.reader = reader;
    }

    public async Task<IntakeEmailDetail> HandleAsync(GetIntakeEmailDetail query, CancellationToken cancellationToken)
    {
        var entity = await context.IntakeEmails
            .FirstOrDefaultAsync(e => e.IntakeId == query.IntakeId, cancellationToken);

        if (entity is null)
            return new IntakeEmailDetail(query.IntakeId, "", false, Array.Empty<IntakeAttachment>());

        var content = entity.GraphMessageId is not null
            ? await reader.GetAsync(entity.GraphMessageId, cancellationToken)
            : null;

        // No live content (Graph off, message moved/gone, or error): fall back to the stored preview.
        if (content is null)
            return new IntakeEmailDetail(query.IntakeId, entity.BodyPreview, false, Array.Empty<IntakeAttachment>());

        var body = content.IsHtml ? Sanitise(content.Body) : content.Body;
        var attachments = content.Attachments
            .Select(a => new IntakeAttachment(a.Name, a.Size, a.ContentType))
            .ToList()
            .AsReadOnly();

        return new IntakeEmailDetail(query.IntakeId, body, content.IsHtml, attachments);
    }

    // Strip scripts, event handlers, and other active content from the untrusted email HTML before
    // it is ever sent to the browser. Defence-in-depth: the client renders it as raw markup.
    private static string Sanitise(string html) => new HtmlSanitizer().Sanitize(html);
}
