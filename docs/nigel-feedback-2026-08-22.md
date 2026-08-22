# Control Centre — Nigel's feedback, 22 Aug 2026

Source: six photos and seven WhatsApp videos (17:32–17:39). Videos were transcribed and frame-sampled; quotes below are Nigel's words, lightly tidied where the audio was rough.

## What he was trying to do, email by email

| # | Email he had open | What he wanted to happen | What actually happened |
|---|---|---|---|
| 1 | "Re: 64 Ravenswood Avenue – GF fabric: SoW…" — his own internal memo of build-ups to the site manager | Tag it as an internal communication / build-up note for the SM | Internal tab offers only **To-do item**; Actions list has nothing that fits ("Internal needs more drop downs", "actions need stuff") |
| 2 | "Re: Abbot Road" — subbie correspondence (17.32.56) | Open the **Subcontractor Communications** pane and click a past thread to tag this email into it, or create a new one | Pane is a read-only feed; nothing is clickable for tagging; Apply stays greyed ("doesn't free up the Apply button… subcontractor communications is glitching") |
| 3 | Stone-paving survey / brick samples email (17.32.56_1) | Same: tag as subbie comms, ideally scoped to the project | Feed shows **100 emails across every project**; "I can't click… if there isn't one I need to be able to create new"; wants a project filter |
| 4 | Subbie "may come Wednesday" email, already linked to TODO-0072 (17.32.56_2) | Tag to the **bid package** the back-and-forth is about | Internal tab has no bid-package dropdown; Subcontractor tab is **locked** because the thread was first filed as Internal ("it hasn't got a drop down for current bid packages") |
| 5 | Redwan / MGN Drywall render-colour uplift, £1,800 (17.32.56_3) | Raise the work order from the email, send the PO, and send his FYI reply in one go | Got the form filled but couldn't find a "raise/stage" button; Save-as-draft tick confused him ("where's my click button… I need Raise Work Order and then bum, tagged to email") |
| 6 | "1986_6.07_260818 – 17A Abbot Road – Ply issue", long chain with Paul, Mick et al (17.36.12) | See the thread and sweep it with Entire thread = Yes | Thread panel showed **nothing** for that email; a sibling email in the same chain showed the thread fine ("broken entire thread") |
| 7 | "Re: Out standing plumbing works" — Steve Williams, 13-email thread (17.38.59) | Tag as subbie comms *and* relate it to the Request/RFI/tender it's about | Three categories (Chaser / Info request / …) but no way to relate comms to a record ("that needs to relate to that somehow") |
| 8 | Tender enquiry coming in (17.32.55) | "Tender inquiry — boom boom boom" then forward to the QS | Dropdown offers Raise RFI / Raise WO / File Bid Package Tender; he wasn't sure which fits; no "hand to QS" step |

Overall verdict (James, 17:40): "the UI needs to have a rethink… I'm gunna get it all redone." Nigel: "best we do together as there may have been updates I'm missing."

## The issues, grouped into fixes

### A. Subcontractor Communications pane is a dead end (emails 2, 3, 7) — highest impact
Nigel's mental model: the SubComms pane *is* where you tag subbie comms. Ours: tagging lives in System Tags → Subcontractor tab (tick), the pane is a browser. He hit the pane three times and never found the tick.

Fix, in order of value:
1. **Put the tagging ticks in the SubComms pane itself.** When an email is selected, render the "Subcontractor communication" tick and the category ticks at the top of the pane (reusing the same `Picked` list the tags pane mutates). Done = Apply lights up from where he already is.
2. **Scope the feed to the triage-bar project by default** with an "All projects" toggle. It's 100 emails today and will only grow. The API already tags by project via the link backfill stem, so this is a filter on `ListSubcontractorComms`.
3. **Make rows actionable**: each thread row gets "Tag this email here" (stages the same SubComms pick + the row's category) and "Open" (previews opposite). "Create new" is just ticking a category — say so in the empty state and in the row hover.
4. **Let comms relate to a record** (email 7): allow a SubComms tick *and* a record pick (RFI / bid package / WO) together, and show the combination in the chip row. Today they already coexist in `Picked`; what's missing is the UI telling him that's allowed.

### B. Pathway lock blocks legitimate re-filing (email 4) — ALREADY FIXED on main (2026-08-21)
A thread filed as Internal used to lock the Subcontractor tab. The hard lock was removed the day before Nigel recorded this; every tab is now choosable and filing to another side goes through the server's cross-filing confirm at Apply. Nigel was on an older build — nothing further to do beyond getting him onto the current deploy.

### C. Internal tab has nothing to tag with (emails 1, 4)
Only To-do. Nigel wants to tag internal staff emails to bid packages, and to file internal instructions such as build-ups for the site manager.

Fix:
1. Add **Bid Package Invite** and **Work Order** to `InternalTypes` in `SystemTagsPane` (one-line change each; the link providers already exist).
2. Add an **Internal Communications** family mirroring SubComms: a constant virtual record `JPMS/IntComms` with categories *Site instruction / Build-up / Spec note / General*, a browser pane, and a nav row under Internal. Same table-less pattern as `SubcontractorCommsLinkProvider` — no migration.

### D. Raise Work Order from an email needs a clear finish (email 5)
The form was complete but he couldn't tell how to commit it, and the Save-as-draft copy read as though it might *stop* the PO going out.

Fix:
1. Add an explicit **"Line up: Raise work order"** button at the foot of `StagedRecordActionEditor` (currently staging is implicit "scratch-until-typed"). Disabled with the `WorkOrderProblem` text until the form is valid; on click it stages and collapses to the chip. Same for RFI / Bid package / Defect.
2. Re-word the PO box to lead with the outcome: "Apply will raise WO, email the PO to redwan@…, tag this email, and send your reply." Move Save-as-draft below, phrased "Hold as a draft instead (no PO email until approved)".
3. Apply summary (`PendingSummary`) should list *reply + WO + PO email* together so he sees the "FYI I'm raising this work order" reply goes in the same apply.

### E. Thread detection silently fails (email 6)
`LoadThreadAsync` keys on Graph `ConversationId` and swallows every exception (`catch { }`) — a failed fetch or a split conversation renders as "no thread", and Entire thread shows no count.

Fix:
1. Surface the failure: replace the bare catch with a `threadError` string rendered where the thread list would be ("Couldn't read the thread — retry").
2. Fallback grouping when `ConversationId` yields one email: match on normalised subject (strip Re:/FW:/Fwd:, the `1986_6.07_260818` drawing prefixes) + overlapping participants within 60 days, server-side in `ListConversationLive`.
3. Show "Entire thread · N emails" even when N comes from the fallback, and mark it "(matched by subject)".

### F. System Actions dropdown — fit for the tender flow (email 8)
Fourteen flat rows; Nigel couldn't pick between Raise RFI / File Bid Package Tender / to-do for an incoming tender, and wanted a "pass to QS" step.

Fix:
1. Group the dropdown with `<optgroup>`: *Raise something new* / *Move an existing record on* / *People & to-dos*.
2. Add a short "When to use" line under the chosen action (one sentence per kind, e.g. File Bid Package Tender: "A subbie has returned pricing for a package you invited them to").
3. New action **Forward to QS** = staged forward to the project's QS contact with the email and attachments, tagged to the bid package. This is a Compose-pane preset rather than a new command.

### G. Housekeeping the videos surfaced
- "Project matched from the email" banner and the Apply hint paragraph take three lines above the split on every email; collapse to one line once he's seen them (per-user dismissed flag).
- Out-of-office auto-replies (James Clark "Automatic reply") sit in the queue; auto-discard `Automatic reply:` / `X-Auto-Response-Suppress` messages into Tagged with a "auto-reply" tag so they never need triage.
- "Recently processed (8)" collapsed section is fine; no action.

## Done in the 2026-08-22 evening round (this commit)
- A1–A3: `SubcontractorCommsTicks.razor` (shared ticks), ticks + "Tag open email like this" + "This project / All projects" scope in `SubcontractorCommsPane.razor` (`ProjectNameMatch.cs`), `OnTagLike` on `CorrespondenceThreadList`, page wiring in `TriageQueue.razor`.
- C1: Internal tab now offers To-do, Bid Package Invite and Work Order (`SystemTagsPane.razor`).
- D: `StagedCreateFooter.razor` under every record-create form (status line, Create now, Done), `CreateProblem`/`Outcome` on `StagedRecordCreate`, outcome-first PO copy in `StagedRecordActionEditor.razor`.
- E: thread read falls back to subject matching when Outlook's conversation id has split (`ConversationSubject.cs`, `ConversationBySubject.cs`, `MailboxPage.MatchedBySubject`, `ListConversationMessages.Subject`); the page now shows a read failure with Retry and flags a subject-matched thread. Note: Apply's whole-thread sweep still follows the Outlook id — the flag says so.

## Suggested order (remaining)
1. A1–A3 (SubComms pane becomes the tagging surface) — this alone fixes three of the seven videos.
2. C1 (Internal tab types) and B (unlock) — two small changes, both unblock email 4.
3. D (WO finish button + copy) — he does this flow daily.
4. E (thread fallback + error) — correctness bug.
5. C2, F, G — UI-rethink items to do together with Nigel, per his "let's do it together" message.

Items 1–4 are contained changes in `SystemTagsPane.razor`, `SubcontractorCommsPane.razor`, `StagedRecordActionEditor.razor` and `TriageQueue.razor`'s `LoadThreadAsync`, plus one API filter; nothing needs a migration.
