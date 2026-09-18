using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Sales.Inbox;

/// <summary>
/// The mailbox-intake Graph classes instantiated a second time for the sales address, built once
/// and shared: the window onto the inbox (<see cref="GraphSalesMailbox"/>) reads through them, and
/// the door replies leave by (<see cref="SalesOutboundEmail"/>) stages through the same client on
/// the same token — so reading the thread and answering it are one mailbox, not two.
/// </summary>
public sealed class SalesMailboxGraph
{
    private SalesMailboxGraph(MailboxGraphClient client, GraphIntakeMessageReader reader, string address)
    {
        Client = client;
        Reader = reader;
        Bodies = new InboundEmailBodyBuilder(reader);
        Address = address;
    }

    public MailboxGraphClient Client { get; }
    public GraphIntakeMessageReader Reader { get; }
    public InboundEmailBodyBuilder Bodies { get; }
    public string Address { get; }

    /// <summary>The projects mailbox's credentials pointed at the sales address: these classes
    /// read the mailbox from the options they are handed, so neither needed changing.</summary>
    public static SalesMailboxGraph For(IConfiguration configuration, string address, IServiceProvider services)
    {
        var options = MailboxIntakeOptions.FromConfiguration(configuration);
        options.Mailbox = address;
        var http = new HttpClient();
        var tokens = new GraphTokenProvider(options);
        return new SalesMailboxGraph(
            new MailboxGraphClient(http, tokens, options, services.GetRequiredService<ILogger<MailboxGraphClient>>()),
            new GraphIntakeMessageReader(http, tokens, options, services.GetRequiredService<ILogger<GraphIntakeMessageReader>>()),
            address);
    }
}
