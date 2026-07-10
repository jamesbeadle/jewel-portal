using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>
/// Raises a valuation invoice in the Raised state — or, for a manual (historic) entry, records it
/// directly as Issued/Paid with backdated dates so receipts-to-date can be brought current.
/// Manual entries count toward "Certified to date" immediately, so any Preapproved claim's frozen
/// totals are re-frozen after the save.
/// </summary>
public sealed class CreateValuationInvoiceHandler : ICommandHandler<CreateValuationInvoice, ValuationInvoice>
{
    private readonly JpmsContext context;
    public CreateValuationInvoiceHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationInvoice> HandleAsync(CreateValuationInvoice command, CancellationToken cancellationToken)
    {
        var project = await context.Projects.FindAsync(new object[] { command.ProjectId }, cancellationToken);
        if (project is null) throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var nextNumber = (await context.ValuationInvoices
            .Where(call => call.ProjectId == command.ProjectId)
            .MaxAsync(call => (int?)call.Number, cancellationToken) ?? 0) + 1;

        var entity = new ValuationInvoiceEntity
        {
            ValuationInvoiceId = ValuationInvoicesIdentifierFactory.NextValuationInvoiceId(),
            ProjectId = command.ProjectId,
            ValuationClaimId = command.ValuationClaimId,
            Number = nextNumber,
            Reference = ValuationInvoicesIdentifierFactory.Reference(nextNumber),
            PeriodMonth = command.PeriodMonth,
            Amount = command.Amount,
            AmountPaid = 0m,
            Status = (int)ValuationInvoiceStatus.Raised,
            RaisedAt = DateTimeOffset.UtcNow,
            IsManual = command.IsManual
        };

        if (command.IsManual)
        {
            // A historic entry: the invoice was really issued (and possibly paid) back then.
            var issuedAt = command.IssuedAt ?? command.PeriodMonth;
            var isPaid = command.PaidAt is not null || (command.AmountPaid ?? 0m) > 0m;
            var amountPaid = isPaid ? command.AmountPaid ?? command.Amount : 0m;

            entity.RaisedAt = issuedAt;
            entity.IssuedAt = issuedAt;
            entity.Status = (int)(isPaid ? ValuationInvoiceStatus.Paid : ValuationInvoiceStatus.Issued);
            entity.AmountPaid = amountPaid;
            entity.PaidAt = isPaid ? command.PaidAt ?? issuedAt : null;

            project.ValuationInvoicePaidTotal += amountPaid;

            ValuationInvoiceAuditTrail.Append(context, entity.ValuationInvoiceId,
                ValuationInvoiceEventType.ManualEntry,
                string.IsNullOrWhiteSpace(command.Note)
                    ? "Historic invoice entered manually."
                    : command.Note,
                amountAfter: command.Amount);
        }
        else
        {
            ValuationInvoiceAuditTrail.Append(context, entity.ValuationInvoiceId,
                ValuationInvoiceEventType.Created, command.Note ?? "", amountAfter: command.Amount);
        }

        context.ValuationInvoices.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        // A manual entry is Issued/Paid from the start, so "Certified to date" just moved.
        if (command.IsManual)
            await PreapprovedClaimTotals.RefreshAsync(context, entity.ProjectId, cancellationToken);

        return entity.ToModel();
    }
}
