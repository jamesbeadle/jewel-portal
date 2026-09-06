using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Features.Sales;

/// <summary>
/// The lead register (Sales → Leads, 2026-09-06): one company-wide list, newest-captured first;
/// the page filters by stage, strategy and text client-side. Nullable Current = no fetch has
/// landed yet (gate on it).
/// </summary>
public sealed class LeadListReadModel : IReadModelStore<IReadOnlyList<Lead>>
{
    private readonly IQueryClient queries;
    public LeadListReadModel(IQueryClient queries) { this.queries = queries; }

    public IReadOnlyList<Lead>? Current { get; private set; }
    public event Action? OnChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListLeads(), cancellationToken);
        OnChanged?.Invoke();
    }
}

/// <summary>One lead with its timeline, keyed by lead id — the lead page's read. LoadedFor says
/// whether a fetch has landed for the key (a null value then means "no such lead").</summary>
public sealed class LeadDetailReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, LeadDetail?> byLead = new();
    public LeadDetailReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public LeadDetail? Current(string leadId) => byLead.TryGetValue(leadId, out var detail) ? detail : null;
    public bool LoadedFor(string leadId) => byLead.ContainsKey(leadId);

    public async Task RefreshAsync(string leadId, CancellationToken cancellationToken)
    {
        byLead[leadId] = await queries.AskAsync(new GetLead(leadId), cancellationToken);
        OnChanged?.Invoke();
    }
}

/// <summary>The strategies with their funnels — the Strategies page and the lead form's
/// strategy picker. Nullable Current = not fetched yet.</summary>
public sealed class SalesStrategyListReadModel : IReadModelStore<IReadOnlyList<SalesStrategyOverview>>
{
    private readonly IQueryClient queries;
    public SalesStrategyListReadModel(IQueryClient queries) { this.queries = queries; }

    public IReadOnlyList<SalesStrategyOverview>? Current { get; private set; }
    public event Action? OnChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListSalesStrategies(), cancellationToken);
        OnChanged?.Invoke();
    }
}

/// <summary>One strategy with its funnel and leads, keyed by strategy id — the strategy page.</summary>
public sealed class SalesStrategyDetailReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, SalesStrategyDetail?> byStrategy = new();
    public SalesStrategyDetailReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public SalesStrategyDetail? Current(string strategyId) => byStrategy.TryGetValue(strategyId, out var detail) ? detail : null;
    public bool LoadedFor(string strategyId) => byStrategy.ContainsKey(strategyId);

    public async Task RefreshAsync(string strategyId, CancellationToken cancellationToken)
    {
        byStrategy[strategyId] = await queries.AskAsync(new GetSalesStrategy(strategyId), cancellationToken);
        OnChanged?.Invoke();
    }
}

/// <summary>
/// The sales inbox (Sales → Inbox, 2026-09-06): one page of sales@ as it stands, the leads its
/// senders match, and the opened thread with each member's body as it is fetched. Nullable
/// Current = no fetch has landed yet (gate on it); a refresh with a search term reads a search
/// page instead of the Inbox page.
/// </summary>
public sealed class SalesInboxReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, MailboxPage> conversations = new();
    private readonly Dictionary<string, MailboxMessageDetail> details = new();
    public SalesInboxReadModel(IQueryClient queries) { this.queries = queries; }

    public SalesInboxPage? Current { get; private set; }
    public string? Cursor { get; private set; }
    public string Search { get; private set; } = "";
    public event Action? OnChanged;

    public async Task RefreshAsync(string? cursor, string search, CancellationToken cancellationToken)
    {
        Cursor = cursor;
        Search = search;
        Current = await queries.AskAsync(new ListSalesInbox(cursor, 25, true, string.IsNullOrWhiteSpace(search) ? null : search), cancellationToken);
        OnChanged?.Invoke();
    }

    public SalesInboxLeadMatch? MatchFor(string email) =>
        Current?.Matches.FirstOrDefault(match => string.Equals(match.Email, email?.Trim(), StringComparison.OrdinalIgnoreCase));

    public MailboxPage? Conversation(string conversationId) =>
        conversations.TryGetValue(conversationId, out var page) ? page : null;

    public async Task LoadConversationAsync(string conversationId, CancellationToken cancellationToken)
    {
        conversations[conversationId] = await queries.AskAsync(new GetSalesInboxConversation(conversationId), cancellationToken);
        OnChanged?.Invoke();
    }

    public MailboxMessageDetail? Detail(string messageId) =>
        details.TryGetValue(messageId, out var detail) ? detail : null;

    public async Task LoadDetailAsync(string messageId, CancellationToken cancellationToken)
    {
        if (details.ContainsKey(messageId)) return;
        details[messageId] = await queries.AskAsync(new GetSalesInboxMessage(messageId), cancellationToken);
        OnChanged?.Invoke();
    }

    public void ForgetConversation(string conversationId) => conversations.Remove(conversationId);
}
