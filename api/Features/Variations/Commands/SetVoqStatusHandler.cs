using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Moves a VOQ between the side-effect-free stages (Draft, Inviting, Tendering, Selected,
/// Rejected). The two commercial transitions stay with their own commands: Approved is only
/// reached through ApproveVariationOrderQuote (which raises the VO and writes the figures) and
/// only left through ReturnVoqToTendering (which reverses them). Selected can only be restored
/// when a winning tender is already recorded on the VOQ; moving back before Selected keeps the
/// recorded tender as history so it can be re-instated.
/// </summary>
public sealed class SetVoqStatusHandler : ICommandHandler<SetVoqStatus, VariationOrderQuote>
{
    private readonly JpmsContext context;
    public SetVoqStatusHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrderQuote> HandleAsync(SetVoqStatus command, CancellationToken cancellationToken)
    {
        var voq = await context.VariationOrderQuotes.FindAsync(new object[] { command.VariationOrderQuoteId }, cancellationToken);
        if (voq is null) throw new InvalidOperationException($"VOQ {command.VariationOrderQuoteId} not found.");

        if (voq.Status == (int)command.Status) return voq.ToModel();

        if (command.Status == VariationOrderQuoteStatus.Approved)
            throw new InvalidOperationException("Approving a VOQ raises a Variation Order and writes the contract figures — use the approve flow.");

        if (voq.Status == (int)VariationOrderQuoteStatus.Approved)
            throw new InvalidOperationException("An approved VOQ can only be un-approved by returning it to tendering, which reverses the approval's commercial writes.");

        if (command.Status == VariationOrderQuoteStatus.Selected && string.IsNullOrEmpty(voq.SelectedBidPackageId))
            throw new InvalidOperationException("No winning tender is recorded on this VOQ — record a selection to mark it Selected.");

        voq.Status = (int)command.Status;

        await context.SaveChangesAsync(cancellationToken);
        return voq.ToModel();
    }
}
