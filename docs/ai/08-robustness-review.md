# 08 — Assistant robustness review: where the turn-based chat breaks, and the plan

*Written 2026-08-26 after a full day of Nigel's testing against the live portal. This gathers every
failure mode seen — the ones already fixed and the ones still open — into one picture, names the
single pattern underneath most of them, and sets an order to work through what remains. It is the
reference for "why does the chat still go wrong sometimes", not a design for one feature.*

## The one pattern underneath most failures

Almost every failure Nigel hit is a version of the same thing: **the assistant asserts an outcome it
has not verified.** It says a file was truncated before it read it; it says a dialog is filled when
the fill never landed; it says a value is corrected when all it did was open a form; it narrates a
navigation that went to a dead page. Each individual case has been patched as it appeared, but the
pattern is the target. A robust turn-based assistant only ever tells the user what it can *see*
happened — a tool result, a validated action, the dialog's own echoed-back state — and says plainly
what it could not do rather than dressing a plan up as a result.

The architecture already has the right instinct in places: `navigate_to` is resolved and rewritten
server-side, `open_modal` is validated before it is handed over, `select_email` and the triage
stages report back whether a page consumed them. The work is finishing that discipline everywhere,
and teaching the model to distrust its own optimism.

## The failures, by class

### A. Slow turns time out — FIXED (docs/ai/07)

The 45-second Static Web Apps gateway cut any Claude call that ran long, so Fable on a cold cache
with a big attachment failed before it could answer. The call now runs on a background task and the
request collects the answer; any model, any latency, completes. *Status: shipped, pending the
`AiPendingReplies` table being created in prod and the deploy.*

### B. A dialog action re-opens itself in a loop — FIXED (docs/ai/06 re-entry guard)

After filling the V2 build-up, the model called `open_modal` for the dialog it was already in; the
page treated that as a fresh request, restarted the task, queued a fresh kick-off, and the kick-off
started a fresh billed conversation that did it again — three conversations a minute apart. The
server now refuses an `open_modal` (and a `navigate_to` carrying `openModal=`) for the dialog the
task already names, and `AiTaskState` never queues a second kick-off for a live task. *Status:
shipped.*

### C. "It thought it had updated the form but it hadn't" — FIXED this pass

`update_open_modal` was the last Ui action with no confirmation path. The server returned
`ok / handed_to_browser` the instant it handed the draft over; on the client the draft was pushed
into an event and, if no live dialog consumed it (wrong page, a panel not yet rendered, a dialog
that had just closed in the loop above), nothing said so — and the model, reading only the server's
`ok`, announced "Done, staged nine lines, total £1,826" over a form still showing £2,454.

Two fixes, matching how the other Ui actions were hardened:

- **Mechanical.** `AiTaskState.ApplyFromAssistant` now returns whether a live dialog actually took
  the update (detected at the point every dialog republishes its state after applying). The panel
  surfaces a plain error when it did not, so the user is never left looking at an unchanged form
  under a "done" message.
- **Behavioural.** The task prompt now forbids "staged", "done", "saved" for a dialog fill; states
  that the form is filled *only* if `update_open_modal` was called this turn; and requires the model
  to VERIFY against the "dialog contents" block on its next turn — the form as it actually stands —
  before claiming any total or line count. If the block does not echo its figures, the fill did not
  land and it must send them again, not assert success.

*Status: shipped this pass. This is the direct fix for images 1–3 of the 2026-08-26 report.*

### D. The attachment doesn't follow the user into a task conversation — OPEN

Nigel attached Valuation No.14, then in a build-up task conversation the model said "I cannot read
the Valuation-No.14 file … it is not attached to this chat." A task starts a *fresh* conversation
and the previous conversation's tail is carried over as a handover, including the most recent few
Context rows (the attachments). But the handover carries only the last three Context rows and only
from the *immediately* previous conversation — so a file attached two conversations back, or behind
newer context, is out of reach, and the model correctly but unhelpfully reports it cannot see it.

*Plan:* a task conversation about a record should be able to reach any file attached anywhere in its
lineage, not just the last handover. Two options, cheapest first: (1) widen the handover to carry
every attachment manifest from the previous conversation (manifests are small — name, sheets, row
counts — even if the bytes are not re-previewed), so `list_sources` / `read_source` can still open
them by handle; (2) make `list_sources` search attachments across the whole conversation *lineage*
(the chain of previous-conversation ids), not just the current conversation. (1) closes the reported
case; (2) is the complete fix. Neither is large.

### E. The assistant plans a whole answer, then can't act — the capability gaps — OPEN

Three distinct walls, all landing as "I worked it all out but you have to do it yourself":

1. **No write path for the change the task is about.** "Update V01 to £1,050" — the model read the
   evidence, found the £600 discrepancy correctly, then: "I have no write access … the correction
   goes through *Revise value*." There is no `variation_revise_value` dialog registered, so the
   assistant can tee up the analysis but cannot open the one control that changes it. Same for "match
   the claim %s" — no dialog for the claim-percentage edit it diagnosed.
2. **The role viewing can't open the dialog.** "The assistant asked to open Agreed build-up, but the
   FinanceDirector role can't open it." The model built the entire staging plan, then the client
   refused the open because the *viewing* role lacks the gate — after the work, not before it.
3. **The right dialog exists but on a record the model can't reach from here.**

*Plan, in priority order:*
- **Register the missing write dialogs as modals** so the assistant can finish the jobs it is
  already trusted to plan: `variation_revise_value` (post-approval value correction) and the
  claim-% editor are the two Nigel actually hit. Each is an existing page control; wiring it into
  `ModalCatalog` + the page's task plumbing is the same pattern as the build-up dialog already
  follows. This is the biggest lever on "a working chat that does things" — most "I can't, you do
  it" answers are a missing modal, not a missing brain.
- **Check the role gate BEFORE the model plans.** The turn context should tell the model which
  dialogs the *current viewing role* can actually open, so it never spends a turn (and the user's
  patience) drafting a staging it cannot deliver — and instead says up front "as Finance Director you
  can review this, but staging needs a QS/commercial role." `ModalCatalog.For(roles)` already exists;
  it needs to feed the task-capability line in the prompt, keyed to the *viewing* role, not just tool
  visibility.

### F. Failure messages that don't say what to do — MOSTLY FIXED

The attachment 500 said "see the inner exception"; the turn 500s said nothing useful. Both now read
the innermost exception and name a missing migration in plain words. *Status: shipped.* Remaining:
the "took longer than one request allows" message is now obsolete for most cases (B is fixed) — once
C and the deploy land, that wording should only ever appear on a genuine >3½-minute model stall, and
its text should be revisited so it does not imply the model is at fault when it is not.

## The order to work through what's open

1. **Deploy what's shipped** — the `AiPendingReplies` table, the endpoint fixes, the loop guard, the
   `update_open_modal` confirmation. Several of these fixes cannot help until they are live, and D/E
   are hard to judge against an old build.
2. **D — attachment lineage** (small, high daily annoyance): widen the handover to carry attachment
   manifests, then let `list_sources` walk the lineage.
3. **E1 — register `variation_revise_value` and the claim-% dialog** (medium, highest capability
   payoff): turns two flagship "I can't" answers into "done, review and press Save".
4. **E2 — role-aware task capability in the prompt** (small): stops wasted turns and wrong promises.
5. **F — retire the obsolete timeout wording** (tiny): once B/C are proven live.

## The standing rule for every future change here

Before the assistant tells the user something happened, it must have seen it happen — a tool result,
a server-validated action, or the dialog's own state echoed back. A plan is not a result, an `ok`
from "handed to the browser" is not a fill, and a preview is not the whole file. Every new tool or
dialog ships with the answer to "how does the model *know* it worked, and what does it say when it
didn't." That is the difference between a chat that occasionally embarrasses itself and one Nigel can
trust to run the portal.
