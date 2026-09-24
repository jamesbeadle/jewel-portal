using System.Reflection;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Requests.Queries;
using Jewel.JPMS.Api.Features.Variations.Queries;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// 2026-09-24: an architect opening an RFI was handed the tagged mail and the internal notes,
// because the 22 Sep rule gated the record-mail endpoints by name and the RFI conversation merged
// the same mail under another name. The rule now sits on the data: the mail readers and the
// conversation reads refuse every external login — client, architect, subcontractor, site
// operative — whatever endpoint reaches them; and no read gate of the internal records admits one.
public sealed class ExternalCorrespondenceTests
{
    private const string Request = "req-1";
    private const string Variation = "vo-1";

    public static TheoryData<Role> ExternalRoles => new()
    {
        Role.Client, Role.Architect, Role.Subcontractor, Role.SiteOperative
    };

    [Theory]
    [MemberData(nameof(ExternalRoles))]
    public void AnExternalLogin_mayNotReadInternalCorrespondence(Role external)
    {
        Assert.False(CallerWith(external).MayReadInternalCorrespondence);
    }

    [Fact]
    public void TheInternalTeam_andTheSystemItself_mayRead()
    {
        Assert.True(CallerWith(Role.ProjectManager).MayReadInternalCorrespondence);
        Assert.True(CallerWith(Role.Architect, Role.ManagingDirector).MayReadInternalCorrespondence);
        Assert.True(new SignedInCaller().MayReadInternalCorrespondence);
    }

    [Theory]
    [MemberData(nameof(ExternalRoles))]
    public void NoReadOfTheInternalRecords_admitsAnExternalLogin(Role external)
    {
        var externalLogin = new[] { external };
        Assert.False(JpmsRoleSets.ProjectDeliveryTeam.IncludesAny(externalLogin));
        Assert.False(RecordEmailRoles.Readers.IncludesAny(externalLogin));
        Assert.False(ArchitectInstructionRoles.AllowedToRead.IncludesAny(externalLogin));
        Assert.False(ArchitectInstructionRoles.AllowedToImportFromMail.IncludesAny(externalLogin));
    }

    [Fact]
    public void TheArchitect_readsNoDrawingThroughTheUnscopedRegister()
    {
        Assert.False(JpmsRoleSets.DrawingReaders.Includes(Role.Architect));
    }

    [Theory]
    [MemberData(nameof(ExternalRoles))]
    public async Task TheRequestMailReader_neverAsksTheMailboxForAnExternalLogin(Role external)
    {
        await using var context = await SeededContextAsync();
        var reader = new RequestEmailReader(context, MailboxThatMustNotBeAsked(), CallerWith(external));

        Assert.Empty(await reader.ForRequestAsync(Request, CancellationToken.None));
    }

    [Theory]
    [MemberData(nameof(ExternalRoles))]
    public async Task AnExternalLogin_readsTheSharedRequestThreadAlone(Role external)
    {
        await using var context = await SeededContextAsync();
        var caller = CallerWith(external);
        var handler = new ListRequestMessagesHandler(context,
            new RequestEmailReader(context, MailboxThatMustNotBeAsked(), caller),
            new MailboxIntakeOptions(), caller);

        var messages = await handler.HandleAsync(new ListRequestMessages(Request), CancellationToken.None);

        Assert.Equal(new[] { "shared" }, messages.Select(message => message.MessageId));
    }

    [Theory]
    [MemberData(nameof(ExternalRoles))]
    public async Task AnExternalLogin_readsTheSharedVariationThreadAlone(Role external)
    {
        await using var context = await SeededContextAsync();
        var handler = new ListVariationOrderMessagesHandler(context, CallerWith(external));

        var messages = await handler.HandleAsync(new ListVariationOrderMessages(Variation), CancellationToken.None);

        Assert.Equal(new[] { "vo-shared" }, messages.Select(message => message.MessageId));
    }

    [Fact]
    public async Task TheInternalTeam_readsTheWholeVariationThread()
    {
        await using var context = await SeededContextAsync();
        var handler = new ListVariationOrderMessagesHandler(context, CallerWith(Role.ProjectManager));

        var messages = await handler.HandleAsync(new ListVariationOrderMessages(Variation), CancellationToken.None);

        Assert.Equal(2, messages.Count);
    }

    private static SignedInCaller CallerWith(params Role[] roles)
    {
        var caller = new SignedInCaller();
        caller.Is(new SignedInUser("someone@example.com", "Someone", roles));
        return caller;
    }

    private static IMailboxGraphClient MailboxThatMustNotBeAsked() =>
        DispatchProxy.Create<IMailboxGraphClient, RefusingProxy>();

    public class RefusingProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The mailbox was asked ({targetMethod?.Name}) for an external login.");
    }

    private static async Task<JpmsContext> SeededContextAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"external-correspondence-{Guid.NewGuid():N}").Options);
        var postedAt = DateTimeOffset.UtcNow;
        context.Requests.Add(new RequestEntity { RequestId = Request, ProjectId = "P-1" });
        context.RequestMessages.AddRange(
            RequestNote("internal", MessageVisibility.Internal, MessageDirection.System, postedAt),
            RequestNote("shared", MessageVisibility.Shared, MessageDirection.System, postedAt.AddMinutes(1)),
            RequestNote("sent-email", MessageVisibility.Shared, MessageDirection.Outbound, postedAt.AddMinutes(2)));
        context.VariationOrderMessages.AddRange(
            VariationNote("vo-internal", MessageVisibility.Internal, postedAt),
            VariationNote("vo-shared", MessageVisibility.Shared, postedAt.AddMinutes(1)));
        await context.SaveChangesAsync();
        return context;
    }

    private static RequestMessageEntity RequestNote(
        string id, MessageVisibility visibility, MessageDirection direction, DateTimeOffset postedAt) => new()
    {
        MessageId = id, RequestId = Request, Body = id, PostedAt = postedAt,
        Visibility = (int)visibility, Direction = (int)direction
    };

    private static VariationOrderMessageEntity VariationNote(
        string id, MessageVisibility visibility, DateTimeOffset postedAt) => new()
    {
        MessageId = id, VariationOrderId = Variation, Body = id, PostedAt = postedAt,
        Visibility = (int)visibility
    };
}
