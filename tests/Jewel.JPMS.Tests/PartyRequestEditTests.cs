using System.Reflection;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Requests.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jewel.JPMS.Tests;

// 2026-09-29, found walking the portal as an architect before the practices were brought on: a
// party reads an RFI with its value and internal notes stripped (PartyReads), so "Record response"
// sent both back as null and the handler wrote them — the first architect to answer an RFI would
// have erased the business's own figure. A party's write keeps the internal fields as they were.
public sealed class PartyRequestEditTests
{
    private const string Project = "P-1";
    private const string TheRequest = "req-1";
    private const decimal TheValue = 1250m;
    private const string TheInternalNotes = "Priced on the QS's rate, not the client's.";

    [Fact]
    public async Task AnArchitectsWrite_keepsTheValueAndTheInternalNotes()
    {
        await using var context = await SeededContextAsync();
        var handler = HandlerFor(context, Role.Architect);

        await handler.HandleAsync(RecordedResponse(), CancellationToken.None);

        var request = await context.Requests.SingleAsync(row => row.RequestId == TheRequest);
        Assert.Equal(TheValue, request.Value);
        Assert.Equal(TheInternalNotes, request.InternalNotes);
        Assert.Equal("The lintel is steel.", request.ResponseText);
    }

    [Fact]
    public async Task TheProjectManagersWrite_replacesThem()
    {
        await using var context = await SeededContextAsync();
        var handler = HandlerFor(context, Role.ProjectManager);

        await handler.HandleAsync(RecordedResponse(), CancellationToken.None);

        var request = await context.Requests.SingleAsync(row => row.RequestId == TheRequest);
        Assert.Null(request.Value);
        Assert.Null(request.InternalNotes);
    }

    private static UpdateRequestDetails RecordedResponse() =>
        new(TheRequest, "RFI-001", "Lintel", "Which lintel?", RequestStatus.Open,
            Value: null, ResponseText: "The lintel is steel.", RespondedByEmail: "architect@practice.example",
            ImpliesVariation: false, InternalNotes: null);

    private static UpdateRequestDetailsHandler HandlerFor(JpmsContext context, Role role)
    {
        var caller = new SignedInCaller();
        caller.Is(new SignedInUser("someone@example.com", "Someone", new[] { role }));
        return new UpdateRequestDetailsHandler(context, MailboxThatMustNotBeAsked(), caller, NullLogger<UpdateRequestDetailsHandler>.Instance);
    }

    private static IMailboxGraphClient MailboxThatMustNotBeAsked() =>
        DispatchProxy.Create<IMailboxGraphClient, RefusingProxy>();

    public class RefusingProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The mailbox was asked ({targetMethod?.Name}) on an edit that kept its reference.");
    }

    private static async Task<JpmsContext> SeededContextAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"party-request-edit-{Guid.NewGuid():N}").Options);
        context.Projects.Add(new ProjectEntity { ProjectId = Project, Reference = "JBB-2026-001", Stage = (int)ProjectStage.LiveDelivery });
        context.Requests.Add(new RequestEntity
        {
            RequestId = TheRequest, ProjectId = Project, Kind = (int)RequestType.Rfi, Reference = "RFI-001",
            Title = "Lintel", Description = "Which lintel?", Status = (int)RequestStatus.Open,
            Value = TheValue, InternalNotes = TheInternalNotes
        });
        await context.SaveChangesAsync();
        return context;
    }
}
