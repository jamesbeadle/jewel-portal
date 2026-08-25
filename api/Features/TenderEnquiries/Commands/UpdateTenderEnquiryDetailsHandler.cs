using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.TenderEnquiries;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

/// <summary>Rewrites what a person typed about the enquiry. Status, dates of record and the
/// answers are untouched — wording only, at every stage.</summary>
public sealed class UpdateTenderEnquiryDetailsHandler : ICommandHandler<UpdateTenderEnquiryDetails, TenderEnquiry>
{
    private readonly JpmsContext context;

    public UpdateTenderEnquiryDetailsHandler(JpmsContext context) { this.context = context; }

    public async Task<TenderEnquiry> HandleAsync(UpdateTenderEnquiryDetails command, CancellationToken cancellationToken)
    {
        var entity = await context.TenderEnquiries
            .FirstOrDefaultAsync(row => row.TenderEnquiryId == command.TenderEnquiryId, cancellationToken)
            ?? throw new InvalidOperationException($"Tender enquiry '{command.TenderEnquiryId}' not found.");

        TenderEnquiryDetailsRules.Apply(entity, command.Details);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
