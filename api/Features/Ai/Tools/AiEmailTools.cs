namespace Jewel.JPMS.Api.Features.Ai.Tools;

// RETIRED 2026-08-14 — safe to `git rm` this file.
//
// draft_outlook_email put assistant-authored drafts straight into the mailbox's Outlook Drafts
// folder. That broke the "always act through the portal UI" rule the moment it was used in anger:
// the user asked for an email in the chat, and the draft materialised in Outlook where they were
// not looking, with no review surface in the portal.
//
// Its replacement is the Control Centre's own New email composer, registered as the
// "compose_email" dialog in ModalCatalog: the assistant opens it with open_modal, writes the draft
// into the form with update_open_modal, and the user reviews the envelope and body on the page and
// presses Send (or Save as draft) themselves — the same human-presses-the-button contract as every
// other registered dialog (ADR-003), and stronger than ADR-006's draft-only rule because the
// review now happens where the user already is.
//
// Nothing references this class any more (AiToolCatalogue.All no longer concatenates it). The file
// survives only because this change was authored over the device bridge, which cannot delete files.
internal static class AiEmailTools
{
}
