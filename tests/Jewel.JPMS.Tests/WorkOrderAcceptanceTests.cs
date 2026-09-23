using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Procurement.Acceptance;
using Jewel.JPMS.Models;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Jewel.JPMS.Tests;

// 2026-09-23, Nigel's ask: a supplier with no portal login accepts a work order from the link in
// the purchase-order email. One token per order, minted on the first send and re-used; the page
// stamps the name the contact typed and the email the link went to; the rule is the same one the
// logged-in portal door uses.
public sealed partial class WorkOrderAcceptanceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TheToken_isMintedOnce_andReusedOnEveryLaterSend()
    {
        var order = new WorkOrderEntity { WorkOrderId = "wo-1", Status = (int)WorkOrderStatus.Released };
        var first = WorkOrderAcceptanceTokens.MintIfMissing(order, Now);
        var second = WorkOrderAcceptanceTokens.MintIfMissing(order, Now.AddDays(3));
        Assert.Equal(first, second);
        Assert.Equal(Now, order.AcceptanceTokenIssuedAt);
        Assert.True(first.Length >= 40);
    }

    [Fact]
    public void TheLink_landsOnThePublicSite_atThePagesOwnRoute()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["PublicSiteUrl"] = "https://portal.test/" }).Build();
        Assert.Equal("https://portal.test/work-orders/accept/abc", new WorkOrderAcceptanceLinks(configuration).For("abc"));
        Assert.Equal("https://portal.jewelbb.co.uk/work-orders/accept/abc", new WorkOrderAcceptanceLinks(new ConfigurationBuilder().Build()).For("abc"));
    }

    [Fact]
    public void TheParagraph_goesAboveTheSignOff_orAtTheEndWhenThereIsNone()
    {
        var composed = "<p>Hello</p>\n<p>Kind regards,<br/>Jewel Bespoke Build</p>\n";
        var withLink = WorkOrderAcceptanceEmailParagraph.InsertInto(composed, "https://portal.test/work-orders/accept/t");
        Assert.True(withLink.IndexOf("accept/t", StringComparison.Ordinal) < withLink.IndexOf("Kind regards", StringComparison.Ordinal));
        Assert.EndsWith("Jewel Bespoke Build</p>\n", withLink);

        var bare = WorkOrderAcceptanceEmailParagraph.InsertInto("<p>Hello</p>", "https://portal.test/work-orders/accept/t&x");
        Assert.EndsWith("</p>", bare);
        Assert.Contains("accept/t&amp;x", bare);
    }

    [Fact]
    public void TheStamp_takesOnlyAnIssuedOrder_andNeverOverwritesAnAcceptance()
    {
        var draft = new WorkOrderEntity { Status = (int)WorkOrderStatus.Draft };
        Assert.False(WorkOrderAcceptance.TryStamp(draft, "Ann", "ann@farrant.test", Now));
        Assert.Null(draft.AcceptedAt);

        var issued = new WorkOrderEntity { Status = (int)WorkOrderStatus.Released };
        Assert.True(WorkOrderAcceptance.TryStamp(issued, "Ann", "ann@farrant.test", Now));
        Assert.True(WorkOrderAcceptance.TryStamp(issued, "Bob", "bob@farrant.test", Now.AddDays(1)));
        Assert.Equal("Ann", issued.AcceptedByName);
        Assert.Equal(Now, issued.AcceptedAt);
    }

    [Fact]
    public void AnOrder_isAwaitingAcceptance_onlyWhileIssuedAndUnaccepted()
    {
        var issued = Order(WorkOrderStatus.Released);
        Assert.True(issued.IsAwaitingAcceptance);
        Assert.False((issued with { AcceptedAt = Now }).IsAwaitingAcceptance);
        Assert.False(Order(WorkOrderStatus.Complete).IsAwaitingAcceptance);
        Assert.False(Order(WorkOrderStatus.Draft).IsAwaitingAcceptance);
    }

    private static WorkOrder Order(WorkOrderStatus status) =>
        new("wo-1", "P-1", null, "sub-1", 100m, "", Now, "pm@jewelbb.co.uk", 1, "Bricks", status, Now, null);
}
