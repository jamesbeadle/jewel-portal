> **Superseded (2026-08-27).** The in-portal chat this document describes was retired in favour of the MCP connector — see [10-mcp-connector.md](10-mcp-connector.md). Kept as the historical record.

# 09 — The turn-based chat from a height: how a message becomes an answer, and where its robustness comes from

*Written 2026-08-26, at the end of a fortnight in which the assistant went from "a chat that reads
the portal" to "a chat that fills dialogs, reads workbooks part by part, and survives a slow model".
Nearly every change in that fortnight was a response to something Nigel saw go wrong. This document
steps back from the individual fixes (06, 07, 08 each explain one) and describes the whole machine
as it now stands — the lifecycle of a turn, the handful of principles the design leans on, how it
got here, and where the seams still are. It is the map to read before the others, and the place to
judge the next change against.*

## 1. The shape in one paragraph

A user message is answered in **hops**. A hop is exactly one Claude call plus whatever tools that
call asked for. The server (`AiTurnRunner`) runs one hop and stops; the browser (`ChatPanel`) pumps
the next one, and the next, until Claude answers without asking for a tool or the hop budget runs
out. Everything the model knows is rebuilt from the database on every hop — the conversation's rows
*are* the state — and the browser contributes nothing but the user's own words and a description of
where it is standing. Claude never writes to the portal: it reads through tools and it *acts* by
handing the browser a small set of validated Ui actions (go to a page, open a dialog, fill the open
dialog), and the user presses Save. That last sentence is the whole safety model, and every other
mechanism exists to make it feel like the assistant did the job anyway.

## 2. The lifecycle of one turn

What follows is the path a message takes, in order, naming the code so it can be checked.

**Send.** If a file is staged it is uploaded first (`POST ai/attachments`): the bytes go to the
`ai-attachments` blob store and an `AiAttachments` row, and the conversation gains a Context row
holding the file's *manifest* and a two-thousand-character preview — never the whole extract
(an image is the exception: its pixels ride whole, as an image block). Then
`POST ai/messages` carries the text, the model tier the user chose, and the **scope**
(`ChatPanel.BuildScope`): the route, the project in view (URL first, page-published id second), the
record the route names, the page note a pane may have published, the selected email on the Control
Centre, and — when a dialog task is live — the task's key, modal, record, and the dialog's current
contents as JSON.

**Start or continue the conversation.** `SendAiMessageHandler` loads the conversation (scoped to
whoever started it — an id is not a capability) or creates one seeded with the agent the route
implies. A task kick-off starts a *fresh* conversation and writes a one-off **handover** Context row:
the last eight turns of the previous conversation at six hundred characters each, plus its last
three Context rows verbatim. The user row is written, marked `kickoff` when the page authored it.

**Frame the hop.** `AiTurnRunner.FrameAsync` loads every row, takes the highest sequence, and
*derives* how many hops this user message has already spent by counting assistant rows since the
last user row. There is no stored "mid-turn" state anywhere, which is why a failed request can
always be retried without cleaning anything up. The agent in force is the conversation's stamped
key, degraded to the orchestrator if the catalogue no longer knows it or this user may not engage it.

**Assemble what the model sees.** Three pieces, kept deliberately apart for caching. The **system
prompt** (`AiSystemPrompt.Build`) is identity, the agent's fragment, the site map, the house
language, the Never block, the command grammar (with the evidence rule inlined), how-to-work notes,
the agent's pinned skills fresh from the database, the floor re-stated *after* the skills so a
portal-edited skill can never outrank it, and the task block when a dialog is open. The **tools**
(`AiToolCatalogue.For`) are thirty-two, filtered by role, by page (the Control Centre tools only on
the Control Centre), by whether a dialog is open (`update_open_modal` is specialised with that
dialog's own schema or dropped), and by agent. The **turn context** (`BuildTurnContext`) is the
volatile part — page, route, project in view, page note, the "files on hand and read so far" lines,
the look-up budget, and the `--- dialog contents ---` block — and it is appended as a text block on
the *newest* message, after the cache breakpoint, so the system prompt and the whole transcript
prefix stay byte-identical hop to hop and read back from cache.

**Replay the transcript.** `BuildTranscript` turns rows into the Anthropic messages array: Context
rows fold into the next user message as leading text; assistant rows replay with their stored
`tool_use` blocks; tool rows group into `tool_result` blocks paired by id (an orphan pair takes the
whole turn down, so unpaired rows degrade to prose). Before replay, `AiTranscriptBudget` applies two
cost bounds to tool-result bodies only: identical calls to the big readers replay latest-only, and
the whole transcript is stubbed oldest-tool-first down to 110,000 characters. Images are counted as
a short stand-in for the budget and replayed as real image blocks.

**Ask, then collect.** The runner inserts an `AiPendingReplies` row and hands the Claude call to
`AiReplyCollector`, which runs it on a background task with a 210-second budget — not the request's
clock, which the Static Web Apps gateway cuts at about 45 seconds. The request waits up to 20
seconds inline. If the answer lands, the hop completes as if nothing had happened. If not, the
request returns `Pending` with the reply id, the panel polls `POST ai/turn/collect` every 750 ms
(each collect long-polls 20 seconds server-side) for up to five minutes, and whichever request
finds the answer completes the hop. A collect whose transcript has moved on refuses and sets the
reply aside; a row in flight for more than four minutes is declared a timeout; the row's status is
a concurrency token so two collects cannot both apply it.

**Run the tools.** `CompleteAsync` persists the assistant row *with* its tool_use blocks, then walks
each call. A **Read** tool executes server-side and its output becomes a Tool row. `switch_agent`
is intercepted and stamps the conversation for the *next* hop. A **Ui** tool is checked before
anything reaches the browser: arguments that do not parse as JSON are refused as "arrived
truncated — your reply hit its length limit mid-call"; `navigate_to` has its route resolved and rewritten (`{project}` filled from the
project in view, a name or reference looked up and replaced, ambiguity and `openModal=` refused);
`open_modal` is checked for a real dialog, a real record in that dialog's own table, the right
status for the dialog (a build-up only before approval, edit-lines only after), a derivable project,
and the **re-entry guard** — the dialog already open with this task attached is refused towards
`update_open_modal`. What survives is handed to the browser as an `AiUiAction`; for `navigate_to`
the model reads a tool result naming the route and project it lands on, while the other Ui actions
still answer only `ok, handed_to_browser` (see §5). Every refusal is also a Tool row, in front of
the model, so it corrects course instead of narrating.

**Persist and report.** One save writes the assistant row, every Tool row, and the pending row's
`Consumed` status together, so an answer is applied exactly once whichever request got there. One
`AgentActivity` row per hop records tokens (including cache reads and writes), cost, and the steps.
The result carries a status — `Complete`, `NeedsContinue` (tools ran, ask again), `Truncated` (hop
budget of ten spent), `Unavailable` (the API failed; the message says nothing was changed), or
`Pending` — plus the new rows, the Ui actions, and a model note if the reply hit its length limit.

**The browser closes the loop.** `RunHopLoopAsync` folds the result: while more hops are coming, an
assistant sentence is shown as the status line ("Reading the V01 sheet…") rather than a bubble; the
Ui actions are applied in order (`navigate_to` navigates, `open_modal` re-checks the role gate and
navigates with `?openModal=`, `update_open_modal` calls `AiTaskState.ApplyFromAssistant` and shows
an error if no live dialog took the draft, the triage actions dispatch to the page); then the panel
waits for the page to settle, rebuilds the scope — so the *next* hop's turn context reflects the
page the assistant just opened and the dialog as it now stands — and continues. A server refusal on
a collect is shown verbatim with a Retry chip; a failed continue gets one quiet retry and then a
"lost the connection" message with Retry; exhaustion offers "Carry on".

## 3. The principles the design leans on

Five ideas do most of the work, and the fortnight's fixes are all instances of them.

**The transcript is the only state.** Hops spent, what has been read, which tool answered what —
all derived from rows on every hop. Nothing is cached in memory across requests except the
in-flight Claude task, and even that has a row behind it. The payoff is that every failure message
can truthfully say "nothing has been changed, asking again is safe", and that a second instance, a
second tab, or a retry all see the same truth.

**The server validates every act before the browser performs it.** A Ui tool's `ok` used to mean
"posted". Three live failures — a dead "Project not found" page, a dialog that never opened from
the To-dos page, a build-up that re-opened itself three times — each came from the browser refusing
*after* the turn had ended, when the model had already said "done". The rule now is that anything
the server can know (the route resolves, the record exists, the status allows it, the dialog is not
already open) is checked in `ValidateUiActionAsync` and refused in front of the model.

**Volatile facts ride at the tail, stable facts at the head.** Prompt caching is what makes a
ten-hop turn affordable, and it only works if the prefix is byte-stable. So where the user is, what
the dialog holds, and how many look-ups remain are never in the system prompt; they are one text
block on the newest message. The `--- dialog contents ---` block is also the mechanism by which the
model *sees* whether its fill landed — which is why the 08 rule ("verify against the next turn's
dialog contents before claiming a total") could be added without new plumbing.

**Reading is plentiful, writing is human.** Thirty-two tools and not one writes. Big documents are
sources with manifests and parts, read by page or sheet and searched server-side, so nothing is
ever "cut off" — only not yet read. Changes happen by staging into a dialog the user can see and
pressing a button the user owns. This is what lets the assistant be trusted with a Finance
Director's variation register: the worst it can do is fill a form wrongly, visibly.

**Failures are said in the user's terms, with the fact that makes retry safe.** Every refusal names
the right next call for the model; every panel error names what the user should press; the
innermost exception is read so "see the inner exception" never reaches anyone.

## 4. How it got here — the fortnight in order

The commit log from the 15th to the 26th reads as a sequence of live failures, each answered with
the same three-part response: a server-side guard, a line in the prompt, and (from the 25th) a
harness scenario that pins it.

By the 14th the foundations were in: the client-pumped hop loop with its budget of ten and the
look-up line in the turn context, prompt caching with the moving breakpoint, the handover into task
conversations, and the model-tier fitter (the per-hop activity row is older still, from late
July). On the 17th the Control Centre "which project" bug taught the rule that a pane with
off-route selection must *publish its resolved id*, not just prose. On the 21st and 22nd came image attachments as real image blocks, the transcript budget's latest-only set widening
to the record readers (a drafting conversation had been re-paying a 25k email on every hop), the
`open_modal` project guard after the To-dos page failure, the registry drift check that fails boot
if a modal or tool is unlabelled, and the `select_email` case that had been a silent no-op. The 24th fixed the chat wiping itself on task kick-off (the carried-over bubbles now dim
above a divider). The 25th was the big day: `navigate_to` validation and rewriting; the four
phases of context retrieval — attachment bytes kept, sources and parts, the valuation loop's two
readers and two dialogs, filed documents as sources, and the regression harness that runs the real
runner against a scripted Claude; reply collection to defeat the 45-second gateway; the re-entry
guard after three billed build-up conversations; and the auto-scroll. The 26th closed the last
unconfirmed Ui action — `update_open_modal` now reports whether a live dialog took it — and wrote
the standing rule in 08: *before the assistant says something happened, it must have seen it
happen.*

Two things stand out from the sequence. First, the failures were rarely model failures; they were
places where the plumbing let the model's optimism through. Second, the fixes compound: the
validation added for `navigate_to` on the 25th is the same shape as the record and project checks
`open_modal` gained on the 16th and 21st, and the 26th's fill confirmation is the same shape again
on the client side. The
architecture had the right instinct; the work was finishing it everywhere.

## 5. Where the seams still are

This is the reason to write the document. Reading the whole machine at once shows where the
principles above are not yet applied uniformly, and a few of these are larger than anything in 08's
open list.

### The truth loop runs one way

The server tells the model what it *validated*; the browser tells the *user* what actually
happened; but the browser never tells the *transcript*. When `open_modal` is refused client-side
because the viewing role lacks the gate, when no page is listening for a triage action, or when
`ApplyFromAssistant` returns false, the panel sets a red error (a navigation refused by the
client's last-line path check does not even do that — it returns silently) and the model only
finds out on the next hop — indirectly, through the dialog contents
or page note — and only if there *is* a next hop. If the Ui action was the last thing in the turn
(status `Complete`), the model has already narrated success and the conversation moves on with a
transcript that says the dialog was filled. 08's fix C closed this for one action by client error
plus a prompt rule; the general fix is structural: the panel should report **action outcomes** back
with its next request (the continue or the next send), and the runner should write them as a row
the model reads — "the browser opened `variation_build_up` for V2" or "the browser could not apply
`update_open_modal`: no live dialog". Then the model's next words are read from a result, which is
the standing rule, and the harness can assert it. This is the single change that would generalise
the fortnight's pattern instead of patching its next instance.

### Capability is checked after planning

08's E2 says it for roles, but the shape is wider. The tool list is filtered by role and page, yet
the *dialogs the viewing role may open* are not stated to the model up front, so it plans a staging
it cannot deliver; the validator's fallthrough for a record dialog it has no table check for (every
current dialog is covered — the next one added will not be, silently) lands on the client's own
not-found handling; and `AgentDefinition.ModalKeys` is declared and populated but read by nothing,
so an agent's dialog scope is not enforced at all. The remedy is one line in the
turn context — "as this role you can open: …" from `ModalCatalog.For(roles)` — plus either using
`ModalKeys` or deleting it.

### Two registries are hand-synchronised where a check could hold them

`AiRegistryDriftCheck` is the right idea and already catches unlabelled modals and any source tool
the evidence rule forgot. It does not cover the "One of: …" modal list inside `open_modal`'s
`modal_key` description (the modal list is stated three times and two are checked), nor the pair of
role lists — `AiRoles.AllowedToUseAssistant` and `AgentCatalogue.CommercialTeam` — that a comment
says mirror each other and that are built from the same `Role` enum through two different
containers and alias spellings. Each is a five-line assertion, and each is a deploy-time
surprise waiting to happen.

### The client loop has timing guesses and a silent edge

The panel waits for a kick-off's first draft by polling ten times at 60 ms and for an opened modal
twenty times at 100 ms; both fail open. Its hop loop runs twelve iterations against the server's
ten and, if it ever falls out still `NeedsContinue`, shows nothing. A collect failure that is not a
server refusal escapes into `Send`'s catch and is reported as "Lost the connection sending that"
when the send succeeded. None of these have bitten yet; all of them will read as "the chat just
stopped" when they do. The drafts-published signal should be an event the page raises, the loop
exhaustion should say so, and the collect path should own its own wording.

### Handover is a window, not a lineage

A task conversation inherits eight turns and three Context rows from the immediately previous
conversation. Two conversations back, or under newer context, an attached file is invisible and the
model correctly says so. 08's D covers this; the durable version is `list_sources` walking the
previous-conversation chain, with manifests carried whole in the handover as the cheap first step.

### The prompt grows by accretion

`AiSystemPrompt` is five hundred lines, and a good share of them are rules added the day a failure
happened. That is the right way to add them and the wrong way to keep them: rules written on
different days for the same failure class drift into near-duplicates, and the model's compliance
with a long list of prohibitions is weaker than with a short list of principles. Nothing here is
wrong today. The suggestion is a periodic consolidation pass — fold the fill-and-verify rules, the
evidence rule and the never-assert rule into one "what you may say and when" section — done with
the harness green before and after, and an extension of the drift check so that any rule naming a
tool or dialog is asserted against the catalogue the way `EvidenceRule` already is. A related cost:
a pinned skill edit invalidates the cached prefix for every conversation on that agent; that is an
acceptable trade for "in force on the next message", but worth knowing when a first hop is slow.

### What can be observed is thinner than what can go wrong

One activity row per hop with tokens and steps is good accounting but not a good forensic trail:
it does not record which Ui actions were handed over, whether the browser performed them, or which
model tier the fitter actually chose (the requested tier sits on the pending row, the step-up only
in the model note the panel shows). The cost figures are zero until the rate settings are
supplied. With action outcomes reported (above) the row could carry them, and a small "turn
inspector" — the rows, the turn context that was sent, the actions and their outcomes — would turn
a screenshot-and-guess report into a two-minute read. The harness, meanwhile, pins every server
behaviour but cannot test the model's own choice of tool; a short post-deploy smoke list of the
flagship asks (load a register by name, read a tab, stage a build-up, fill a reply) is the
complement, and it should live in the docs beside the scenarios it corresponds to.

## 6. The questions to ask of every future change

The standing rule in 08 becomes, in practice, a short list to answer before a tool, dialog or prompt
rule ships. How does the model *know* it worked — which tool result or context block echoes the
outcome? What does the user see when it did not, and does that message name the next step? Is retry
safe, and does the message say so? Is the new name — tool, modal, label — under the drift check?
Which harness scenario pins it, and which live failure is it the answer to? Does it add to the
cached prefix or the volatile tail, and is that the right side? A change that answers all six is in
the spirit of the fortnight; one that cannot answer the first is the next screenshot.

## 7. The order to work through it

The order in 08 stands — deploy what is shipped, then attachment lineage, then the two missing
write dialogs, then role-aware capability, then the timeout wording — with two insertions from this
wider view. Action-outcome reporting belongs immediately after the deploy, because it is the
general form of the fix the last three failures each needed individually and it makes every later
dialog cheaper to trust. The drift-check widening and the client-loop tidy-ups are small enough to
ride along with whichever change touches those files next. The prompt consolidation waits until the
harness has a few more scenarios to protect it.
