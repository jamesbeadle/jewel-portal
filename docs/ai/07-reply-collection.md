> **Superseded (2026-08-27).** The in-portal chat this document describes was retired in favour of the MCP connector — see [10-mcp-connector.md](10-mcp-connector.md). Kept as the historical record.

# 07 — Reply collection: why a hop no longer waits for Claude inside one request

*Written 2026-08-25, after the V01 task failed twice with "That reply took longer than one request
allows" — on Fable, with Valuation No.14 (17 sheets, 1,193 rows) attached. The attachment had
uploaded correctly; the manifest and preview were right; the model never got to say anything.*

## The problem in one sentence

Every hop of a turn runs inside one HTTP request through the Static Web Apps gateway, and that
gateway cuts any API request at about 45 seconds — for managed functions and bring-your-own
functions alike, with no setting to raise it. A Claude call therefore gets 36 seconds
(`ClaudeConversationClient.CallBudget`), and a capable model on a cold cache with a 25,000-token
prompt and a long tool call to write does not always answer in 36 seconds. When it does not, the
hop fails, the panel shows the timeout message, and the user has learned nothing except that the
assistant cannot do the job on the model they chose.

## Why the obvious fixes do not work

Streaming does not help: a streamed response still has to *finish* before the gateway's clock
runs out, and the hop cannot execute tools or persist anything until the last token has landed.
Raising the budget is not available: the 45 seconds belongs to the gateway, not to the function
host. Trimming the prompt helps at the margin — the tool catalogue is the largest single block
and is cached after the first hop of a conversation — but the first hop of every fresh
conversation still pays the whole prefix, and a bigger model is slower per token whatever the
prefix. Telling the user to use Sonnet is a workaround, not a product. Moving the whole hop onto
the worker Function app would fit any model but adds a queue hand-off and a cold start to every
message, and the runner's tools reach into most of the API's feature code — the wrong trade for
a chat that is meant to feel immediate.

## The design: ask, then collect

The Claude call is the only slow thing in a hop. Everything before it (loading the transcript,
building the prompt) and after it (running tools, writing rows) is fast. So the call is moved off
the request's clock and the request *collects* the answer instead of waiting for it.

A hop now runs like this. The runner prepares the prompt exactly as before, inserts an
`AiPendingReplies` row (in flight), and hands the call to the **reply collector** — a singleton
that starts the call on a background task with its own, long budget (three and a half minutes)
rather than the request's cancellation token. The request then waits for that task for up to
twenty seconds. In the common case the answer lands inside the wait and the hop completes as it
always did: tools run, rows are written, the result goes back, and the pending row is marked
consumed. Nothing about a fast hop changes except one extra row.

When the answer has not landed in twenty seconds the request returns a result whose status is
`Pending` and which names the reply id. The background call carries on. The panel, seeing
`Pending`, posts to `ai/turn/collect` with that id; the server waits up to twenty seconds for the
answer (on the in-memory task if this instance owns it, by polling the row if another instance
does) and either completes the hop — the same tool run and the same rows as the fast path — or
answers `Pending` again and the panel asks again. Each poll is a short request well inside the
gateway's limit, so the model can take as long as it needs and the gateway never sees a slow
request.

When the answer arrives, the background task writes it to the row (status answered, the reply as
JSON) before the request that started it, or any later collect, reads it — so a collect served by
a different instance finds the same answer in the database.

## What can go wrong, and what happens

*The instance running the background call dies.* The row stays in flight. A collect that finds an
in-flight row older than four minutes marks it failed and answers with the timeout message the
runner always used — "nothing has been changed, asking again is safe" — because it is still true:
a hop that never answered wrote nothing.

*The user sends another message before the reply is collected.* The row records the transcript's
sequence at the moment the call went out; a collect whose transcript has moved on refuses ("the
conversation moved on before that reply was collected") and marks the row consumed, so a late
answer can never be spliced into a transcript that no longer matches the prompt it was for.

*Two collects race for the same answer.* The row's status carries a concurrency check; the second
writer fails and reports that the reply was already collected. The panel is strictly sequential,
so this needs a second tab pointed at the same conversation.

*Claude fails.* The background task records the failure class (`timeout`, `busy`, `connection`)
on the row and the collect surfaces the same failure message as the inline path — the user sees
no difference between a failure at second five and a failure at second ninety.

## What the user sees

Nothing new on a fast hop. On a slow one the status line keeps its narration ("Reading the V01
sheet…") while the panel polls; there is no chip to press and no message to re-send. The wait is
bounded at five minutes end to end, after which the Retry chip appears with the usual "nothing is
lost" wording. The model choice is honoured whatever the latency: a Fable turn that takes ninety
seconds on a cold cache now completes.

## Status

Implemented 2026-08-25: `AiPendingReplyEntity` + migration `20260825160000_AddAiPendingReplies`
(`api/Migrations/add-ai-pending-replies.sql`), `AiReplyCollector`, the runner's
`RunHopAsync` / `CollectAsync` split, `CollectAiReply` command + `ai/turn/collect` endpoint,
`AiTurnStatus.Pending`, the panel's collect loop, and a harness scenario proving a reply that
outlives the inline wait is collected on a later call with the same tool run as the fast path.
