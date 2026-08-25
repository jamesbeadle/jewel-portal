using System.Collections.Concurrent;
using System.Text.Json;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// The Claude calls in flight — "ask, then collect" (docs/ai/07-reply-collection.md).
///
/// <para>The Static Web Apps gateway cuts any request at ~45s, and a capable model on a cold cache
/// with a big prompt does not always answer in that. So a hop's Claude call runs here, on a
/// background task with its own budget, and the request that started it waits only a bounded
/// while before answering "pending". The answer is written to <c>AiPendingReplies</c> the moment it
/// lands, so a later collect — on this instance (which awaits the task) or another (which reads
/// the row) — finds it and finishes the hop.</para>
///
/// <para>A singleton on purpose: the task must outlive the request that started it, and the
/// per-request scope is gone by the time the answer arrives, so the write goes through a scope of
/// this class's own.</para>
/// </summary>
public sealed class AiReplyCollector
{
    /// <summary>How long a background call may take across attempts. Generous: nothing is waiting
    /// on the request's clock any more, and a long tool call on a big model on a cold cache is
    /// exactly the case this exists for.</summary>
    public static readonly TimeSpan CallBudget = TimeSpan.FromSeconds(210);

    /// <summary>How long a request — the hop that started the call, or a collect — waits for the
    /// answer before returning "pending". Under the gateway's ~45s with room for the tool run and
    /// the writes that follow a reply. Settable so the harness can prove the pending path in
    /// milliseconds rather than waiting twenty seconds.</summary>
    public TimeSpan InlineWait { get; init; } = TimeSpan.FromSeconds(20);

    /// <summary>An in-flight row older than this belongs to an instance that died mid-call. A
    /// collect treats it as a timeout: nothing was written, asking again is safe.</summary>
    public static readonly TimeSpan Deadline = CallBudget + TimeSpan.FromSeconds(30);

    /// <summary>How often a collect served by an instance that does not own the task re-reads the
    /// row during its wait.</summary>
    private static readonly TimeSpan RowPoll = TimeSpan.FromMilliseconds(1_500);

    private readonly ConcurrentDictionary<string, Task<ClaudeReply>> inFlight = new(StringComparer.Ordinal);
    private readonly IServiceScopeFactory scopes;
    private readonly ILogger<AiReplyCollector> logger;

    public AiReplyCollector(IServiceScopeFactory scopes, ILogger<AiReplyCollector> logger)
    {
        this.scopes = scopes;
        this.logger = logger;
    }

    /// <summary>
    /// Starts the call. The pending row must already be saved (in flight) by the caller — the
    /// background task only ever UPDATES it, and a collect on another instance may read it before
    /// the answer lands. <paramref name="call"/> receives the cancellation token to run under; it
    /// is this class's own, never the request's.
    /// </summary>
    public Task<ClaudeReply> Begin(string replyId, Func<CancellationToken, Task<ClaudeReply>> call)
    {
        // Registered BEFORE the call starts, on a completion source, so a reply that lands
        // instantly (the harness's scripted ones) cannot finish and clear its entry before the
        // entry exists — which would leave a completed task pinned for the process lifetime.
        var completion = new TaskCompletionSource<ClaudeReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        inFlight[replyId] = completion.Task;
        _ = RunAsync(replyId, call, completion);
        return completion.Task;
    }

    /// <summary>
    /// Waits up to <paramref name="wait"/> for the reply. On the instance that owns the task this
    /// awaits the task itself; elsewhere it re-reads the row. Null means "still in flight" — the
    /// caller answers pending. A failed call comes back as a reply with <c>Ok == false</c> and the
    /// failure class in <c>Error</c>, exactly as the inline path always saw it.
    /// </summary>
    public async Task<ClaudeReply?> WaitAsync(
        JpmsContext context, AiPendingReplyEntity row, TimeSpan wait, CancellationToken ct)
    {
        if (inFlight.TryGetValue(row.ReplyId, out var task))
        {
            var finished = await Task.WhenAny(task, Task.Delay(wait, ct));
            if (!ReferenceEquals(finished, task)) return null;
            // The task wrote the row before completing; the caller's tracked copy is behind.
            await context.Entry(row).ReloadAsync(ct);
            return await task;
        }

        // Another instance owns the task (or the owner died): the row is the only view.
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            await context.Entry(row).ReloadAsync(ct);
            if (row.Status == AiPendingReplyStatus.Answered && row.ReplyJson is not null)
                return Deserialise(row.ReplyJson);
            if (row.Status == AiPendingReplyStatus.Failed)
                return new ClaudeReply(false, null, Array.Empty<ClaudeToolCall>(), null, row.Error ?? "exception");
            if (row.Status == AiPendingReplyStatus.Consumed)
                return null;

            if (DateTimeOffset.UtcNow - row.RequestedAt > Deadline)
            {
                // The instance running the call is gone. Say so on the row so every later collect
                // agrees, then report it the way a slow generation always was reported.
                row.Status = AiPendingReplyStatus.Failed;
                row.Error = "timeout";
                row.AnsweredAt = DateTimeOffset.UtcNow;
                await context.SaveChangesAsync(ct);
                return new ClaudeReply(false, null, Array.Empty<ClaudeToolCall>(), null, "timeout");
            }

            if (clock.Elapsed + RowPoll > wait) return null;
            await Task.Delay(RowPoll, ct);
        }
    }

    private async Task RunAsync(
        string replyId, Func<CancellationToken, Task<ClaudeReply>> call, TaskCompletionSource<ClaudeReply> completion)
    {
        // Yield so Begin returns to its caller before the call starts — the caller must not
        // block on the first await of the HTTP send.
        await Task.Yield();

        ClaudeReply reply;
        using var cts = new CancellationTokenSource(CallBudget + TimeSpan.FromSeconds(15));
        try
        {
            reply = await call(cts.Token);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Background Claude call {ReplyId} threw.", replyId);
            reply = new ClaudeReply(false, null, Array.Empty<ClaudeToolCall>(), null,
                ex is OperationCanceledException ? "timeout" : "exception");
        }

        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<JpmsContext>();
            var row = await context.AiPendingReplies.FirstOrDefaultAsync(pending => pending.ReplyId == replyId);
            if (row is not null && row.Status == AiPendingReplyStatus.InFlight)
            {
                row.Status = reply.Ok ? AiPendingReplyStatus.Answered : AiPendingReplyStatus.Failed;
                row.ReplyJson = reply.Ok ? Serialise(reply) : null;
                row.Error = reply.Ok ? null : reply.Error;
                row.AnsweredAt = DateTimeOffset.UtcNow;
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            // A request awaiting this task on this instance still gets the reply; a collect that
            // arrives later, here or elsewhere, finds the row still in flight, hits the deadline
            // and reports a timeout — nothing written, retry safe.
            logger.LogError(ex, "Could not record the answer to Claude call {ReplyId}.", replyId);
        }
        finally
        {
            // The row now carries the answer, so the task is no longer the only view of it. Removed
            // here rather than on collect, so a reply nobody collects cannot pin memory for ever —
            // and BEFORE the task completes, so a wait that arrives in between reads the row.
            inFlight.TryRemove(replyId, out _);
            completion.TrySetResult(reply);
        }
    }

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string Serialise(ClaudeReply reply) => JsonSerializer.Serialize(reply, Json);

    public static ClaudeReply Deserialise(string json) =>
        JsonSerializer.Deserialize<ClaudeReply>(json, Json)
        ?? new ClaudeReply(false, null, Array.Empty<ClaudeToolCall>(), null, "exception");
}
