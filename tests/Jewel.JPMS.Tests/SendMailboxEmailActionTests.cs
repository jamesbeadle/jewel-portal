using System.Text.Json;
using Jewel.JPMS.Api.Features.Ai.Tools.Actions;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The connector's send_mailbox_email action (2026-09-04): the Control Centre's Reply box and
// Compose pane, mirrored. Pins the parity promise — an internal role's own AI tool can send from
// the projects mailbox exactly as the page does — and the two guards that make that safe: the
// action is confirm-first, and the sender is stamped server-side so a model can never write it.
public sealed class SendMailboxEmailActionTests
{
    private static AiAction Action => AiActionRegistry.All.Single(action => action.Name == "send_mailbox_email");

    private static SignedInUser UserWith(params Role[] roles) => new("test@jewelbb.co.uk", "Test User", roles);

    private static SendMailboxEmail AnyCommand() => new(
        ReplyToMessageId: "msg-1",
        ReplyToInternetMessageId: null,
        To: new[] { new ComposeRecipient("craig@example.com") },
        Cc: Array.Empty<ComposeRecipient>(),
        Bcc: Array.Empty<ComposeRecipient>(),
        Subject: "RE: Woodhouse Lane",
        Body: "Thanks Craig.",
        BodyIsHtml: false,
        Attachments: Array.Empty<ComposeAttachmentRef>());

    [Fact]
    public void IsDeclared_asTheComposeCommand_behindTheEndpointsGate()
    {
        Assert.Equal(typeof(SendMailboxEmail), Action.CommandType);
        Assert.Equal(typeof(ComposeOutcome), Action.ResultType);
        Assert.Equal(typeof(SendMailboxEmailAuthorisation), Action.AuthorisationType);
        Assert.Null(Action.ValidationType);
        Assert.Equal("Correspondence", Action.Area);
    }

    [Fact]
    public void IsConfirmFirst_andStampsTheSender()
    {
        Assert.True(Action.RequiresConfirmation, "an email cannot be recalled — the first call must be refused");
        Assert.Equal(new[] { "SenderEmail" }, Action.EmailStamps);
    }

    [Fact]
    public void Schema_hidesTheSender_andDescribesTheReplyShape()
    {
        var schema = JsonSerializer.Serialize(AiActionSchema.InputSchema(Action));
        Assert.DoesNotContain("senderEmail", schema);
        Assert.Contains("replyToMessageId", schema);
        Assert.Contains("saveAsDraftOnly", schema);
        Assert.Contains("markThreadHandled", schema);
    }

    [Fact]
    public void IsOffered_toEveryInternalRole_andNeverToExternals()
    {
        Assert.True(Action.VisibleTo.IncludesAny(new[] { Role.ProjectManager }));
        Assert.True(Action.VisibleTo.IncludesAny(new[] { Role.Foreman }));
        Assert.True(Action.VisibleTo.IncludesAny(new[] { Role.Accounts }));
        Assert.False(Action.VisibleTo.IncludesAny(new[] { Role.Subcontractor }));
        Assert.False(Action.VisibleTo.IncludesAny(new[] { Role.Client }));
        Assert.False(Action.VisibleTo.IncludesAny(new[] { Role.Architect }));
    }

    [Fact]
    public void Gate_readsTheSameSetAsTheEndpoint()
    {
        var gate = new SendMailboxEmailAuthorisation();
        Assert.True(gate.Allows(UserWith(Role.SiteManager), AnyCommand()));
        Assert.True(JpmsRoleSets.AllInternal.IncludesAny(UserWith(Role.SiteManager).Roles));
        Assert.False(gate.Allows(UserWith(Role.Subcontractor), AnyCommand()));
        Assert.False(gate.Allows(UserWith(Role.Client), AnyCommand()));
    }
}
