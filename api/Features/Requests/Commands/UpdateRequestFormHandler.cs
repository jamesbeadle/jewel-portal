using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Saves the structured body of a request's official document: the three narrative sections plus
/// the itemised queries. Items are replace-all — the submitted list becomes the request's items in
/// the submitted order (positions re-minted 1..n), so add / edit / remove / reorder in the editor
/// all land as one write. Nothing else on the request is touched, and nothing is sent: the next
/// document render (download, resend, email draft) simply picks the new content up.
/// </summary>
public sealed class UpdateRequestFormHandler : ICommandHandler<UpdateRequestForm, Request>
{
    private readonly JpmsContext context;
    public UpdateRequestFormHandler(JpmsContext context) { this.context = context; }

    public async Task<Request> HandleAsync(UpdateRequestForm command, CancellationToken cancellationToken)
    {
        var entity = await context.Requests.FindAsync(new object[] { command.RequestId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Request {command.RequestId} not found.");

        entity.BasisOfQueries = Clean(command.BasisOfQueries);
        entity.ResponseActionRequired = Clean(command.ResponseActionRequired);
        entity.ImpactIfLate = Clean(command.ImpactIfLate);

        // Replace-all: fully blank rows are dropped (empty editor rows), the rest keep the submitted
        // order. Existing rows are removed and re-inserted with fresh ids — items carry no identity
        // of their own beyond the request they belong to.
        var existing = await context.RequestItems
            .Where(item => item.RequestId == entity.RequestId)
            .ToListAsync(cancellationToken);
        context.RequestItems.RemoveRange(existing);

        var kept = (command.Items ?? Array.Empty<RequestItemDraft>())
            .Where(item => !string.IsNullOrWhiteSpace(item.Query)
                || !string.IsNullOrWhiteSpace(item.DrawingRef)
                || !string.IsNullOrWhiteSpace(item.MemberArea)
                || !string.IsNullOrWhiteSpace(item.Response))
            .ToList();

        var replacements = kept.Select((item, index) => new RequestItemEntity
        {
            RequestItemId = RequestsIdentifierFactory.Next(),
            RequestId = entity.RequestId,
            Position = index + 1,
            DrawingRef = item.DrawingRef?.Trim() ?? "",
            MemberArea = item.MemberArea?.Trim() ?? "",
            Query = item.Query?.Trim() ?? "",
            Response = Clean(item.Response)
        }).ToList();
        context.RequestItems.AddRange(replacements);

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel(replacements);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
