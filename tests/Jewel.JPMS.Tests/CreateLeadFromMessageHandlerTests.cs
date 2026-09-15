using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Sales;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// The Sales pane's "create new" (2026-09-15): the lead is captured through the ordinary
// CaptureLead handler — Engaged, Inbound, no strategy, owned by whoever tags it unless the
// command names someone — and the email is linked to it on the Sales pathway.
public sealed class CreateLeadFromMessageHandlerTests
{
    [Fact]
    public async Task CapturesTheLeadEngagedAndInbound_thenLinksTheEmailOnTheSalesPathway()
    {
        await using var context = NewContext();
        var link = new RecordingLink();
        var handler = new CreateLeadFromMessageHandler(new CaptureLeadHandler(context), link, new AuditActor { Email = "tagger@jewelbb.co.uk" });

        var lead = await handler.HandleAsync(Enquiry(), CancellationToken.None);

        Assert.Equal(LeadStage.Engaged, lead.Stage);
        Assert.Equal(LeadSource.Inbound, lead.Source);
        Assert.Null(lead.StrategyId);
        Assert.Equal("tagger@jewelbb.co.uk", lead.OwnerEmail);
        Assert.Equal("LD-0001", lead.Reference);

        var linked = Assert.Single(link.Received);
        Assert.Equal("msg-1", linked.MessageId);
        Assert.Equal(RecordType.Lead, linked.Type);
        Assert.Equal(lead.LeadId, linked.RecordId);
        Assert.Equal("Sales", linked.Pathway);
        Assert.Equal(LinkThreadScope.EntireThread, linked.Scope);
        Assert.Equal("<abc@example.com>", linked.InternetMessageId);
    }

    [Fact]
    public async Task ANamedOwner_beatsTheTagger()
    {
        await using var context = NewContext();
        var handler = new CreateLeadFromMessageHandler(new CaptureLeadHandler(context), new RecordingLink(), new AuditActor { Email = "tagger@jewelbb.co.uk" });

        var lead = await handler.HandleAsync(Enquiry() with { OwnerEmail = "nigel@jewelbb.co.uk" }, CancellationToken.None);

        Assert.Equal("nigel@jewelbb.co.uk", lead.OwnerEmail);
    }

    private static CreateLeadFromMessage Enquiry() =>
        new("msg-1", "Jane Coombe", "jane@example.com", "", "", LeadProspectKind.Homeowner,
            "12 Coombe Lane", "kt2 7aa", "Rear extension", "", null,
            InternetMessageId: "<abc@example.com>", Scope: LinkThreadScope.EntireThread);

    private static JpmsContext NewContext() =>
        new(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"lead-from-message-{Guid.NewGuid():N}").Options);

    private sealed class RecordingLink : ICommandHandler<LinkMessageToRecord, Acknowledgement>
    {
        public List<LinkMessageToRecord> Received { get; } = new();

        public Task<Acknowledgement> HandleAsync(LinkMessageToRecord command, CancellationToken cancellationToken)
        {
            Received.Add(command);
            return Task.FromResult(new Acknowledgement(command.RecordId));
        }
    }
}
