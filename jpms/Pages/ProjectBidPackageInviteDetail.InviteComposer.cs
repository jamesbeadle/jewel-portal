using System.Text.Json;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Pages;

public partial class ProjectBidPackageInviteDetail
{
    // ---- The invite composer: compose, persist on the package, send from the mailbox ----

    private bool showComposeModal;
    private bool editingBody;
    private bool savingDraft;
    private string composeSubject = "";
    private string composeBody = "";
    private string composeTo = "";
    private string composeCc = "";
    private string composeBcc = "";
    private DateTimeOffset? composeDraftSavedAt;
    private string? sendNote;
    private string? draftWebLink;

    // Invited but unreachable — no email address in the directory.
    private IReadOnlyList<string> MissingEmail() =>
        recipients
            .Select(r => Subs.Find(r.SubcontractorId) is { } s
                ? (Name: s.CompanyName, HasEmail: !string.IsNullOrWhiteSpace(s.ContactEmail))
                : (Name: r.SubcontractorId, HasEmail: false))
            .Where(x => !x.HasEmail)
            .Select(x => x.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    // The tender list's directory emails — the BCC the composer opens with.
    private IReadOnlyList<string> TenderListEmails() =>
        recipients
            .Select(r => Subs.Find(r.SubcontractorId))
            .Where(s => s is not null && !string.IsNullOrWhiteSpace(s.ContactEmail))
            .Select(s => s!.ContactEmail.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static int CountAddresses(string raw) =>
        raw.Split(new[] { ';', ',' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Count(address => address.Contains('@'));

    private int ComposeRecipientCount() =>
        CountAddresses(composeTo) + CountAddresses(composeCc) + CountAddresses(composeBcc);

    private async Task OpenComposeModal()
    {
        if (package is null) return;
        sendNote = null;
        editingBody = false;
        composeDraftSavedAt = null;

        // The persisted draft wins — a half-written invite picked up where it was left, by
        // whoever picks it up. Only when none exists does the composer open on the defaults.
        BidPackageInviteComposerDraft? saved = null;
        try { saved = await Queries.AskAsync(new GetBidPackageInviteComposerDraft(BidPackageId), CancellationToken.None); }
        catch { /* no draft is a fine answer; the defaults stand */ }

        if (saved is not null)
        {
            composeSubject = saved.Subject;
            composeBody = saved.Body;
            composeTo = saved.To;
            composeCc = saved.Cc;
            composeBcc = saved.Bcc;
            composeDraftSavedAt = saved.SavedAt;
        }
        else
        {
            composeSubject = $"Invitation to tender — {package.Title} ({package.Reference})";
            composeBody = DefaultInviteBody();
            composeTo = "";
            composeCc = "";
            composeBcc = string.Join("; ", TenderListEmails());
        }
        showComposeModal = true;
        StateHasChanged();
    }

    private async Task SaveInviteDraft()
    {
        if (savingDraft || package is null || !CanEdit) return;
        try
        {
            savingDraft = true;
            await Commands.SendAsync(
                new SaveBidPackageInviteComposerDraft(BidPackageId, composeSubject.Trim(), composeBody,
                    composeTo.Trim(), composeCc.Trim(), composeBcc.Trim()), CancellationToken.None);
            composeDraftSavedAt = DateTimeOffset.Now;
        }
        catch { error = "Couldn't save the invite draft. Please try again."; }
        finally { savingDraft = false; }
    }

    private void CloseComposeModal()
    {
        if (busy) return;
        showComposeModal = false;
        // Closing keeps the work: the draft persists on the package quietly, best-effort — the
        // user asked for exactly this ("it will be useful later"). A failed save costs a re-type,
        // never an error dialog on the way out.
        if (CanEdit && package is not null)
            _ = Commands.SendAsync(
                new SaveBidPackageInviteComposerDraft(BidPackageId, composeSubject.Trim(), composeBody,
                    composeTo.Trim(), composeCc.Trim(), composeBcc.Trim()), CancellationToken.None);
    }

    // The pre-filled HTML invite: scope summary plus the line items to price, grouped by trade.
    private string DefaultInviteBody()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<p>Hello,</p>");
        if (lineItems.Count > 0)
            sb.AppendLine($"<p>Jewel Bespoke Build invites you to tender for the <strong>{package!.Title}</strong> package (ref {package.Reference}). Please complete and return the attached pricing schedule — the Rate and Total columns are left for you — and reply to this email with your exclusions and lead times, quoting the reference.</p>");
        else
            sb.AppendLine($"<p>Jewel Bespoke Build invites you to tender for the <strong>{package!.Title}</strong> package (ref {package.Reference}). Please price the works described in the attached documents and reply to this email with your rates, exclusions and lead times, quoting the reference in your reply.</p>");
        foreach (var group in lineItems.GroupBy(item => item.Trade).OrderBy(g => g.Key))
        {
            var heading = string.IsNullOrWhiteSpace(group.Key) ? "General" : group.Key;
            sb.AppendLine($"<p><strong>{heading}</strong></p>");
            sb.AppendLine("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse\">");
            sb.AppendLine("<tr><th align=\"left\">Description</th><th align=\"left\">Qty</th><th align=\"left\">Unit</th><th align=\"left\">Rate</th><th align=\"left\">Total</th></tr>");
            foreach (var item in group.OrderBy(i => i.SortOrder))
                sb.AppendLine($"<tr><td>{item.Description}</td><td>{item.Quantity}</td><td>{item.Unit}</td><td></td><td></td></tr>");
            sb.AppendLine("</table>");
        }
        sb.AppendLine("<p><strong>Please include the following with your tender:</strong></p>");
        sb.AppendLine("<ul>");
        if (package!.MaterialsApplicable)
            sb.AppendLine("<li><strong>Materials</strong> — please state whether you will be supplying your own materials for this work. If your rates are supply-and-fit, itemise the materials included; if they are labour-only, state what you expect us to supply.</li>");
        sb.AppendLine("<li><strong>Deposit</strong> — our preference is not to pay a deposit upfront. If one is required for you to take on the work, please state the amount and terms in your tender.</li>");
        sb.AppendLine("<li><strong>Duration</strong> — how long the work will take.</li>");
        sb.AppendLine("<li><strong>Availability</strong> — if selected, when you would be able to start.</li>");
        sb.AppendLine("<li><strong>Insurances</strong> — confirmation that you hold all insurances required for this work.</li>");
        sb.AppendLine("<li><strong>RAMS</strong> — confirmation that you will provide the required RAMS documentation.</li>");
        sb.AppendLine("<li><strong>Portfolio</strong> — examples of prior comparable work.</li>");
        sb.AppendLine("</ul>");
        sb.AppendLine("<p>Our tender terms and conditions are attached; your tender will be taken as made on that basis unless it states otherwise.</p>");
        sb.AppendLine("<p>Please confirm receipt and let us know if you need any further information or drawings.</p>");
        sb.AppendLine("<p>Kind regards,<br/>Jewel Bespoke Build</p>");
        return sb.ToString();
    }

    private async Task ConfirmSendInvite()
    {
        if (busy || package is null || !CanEdit || ComposeRecipientCount() == 0) return;
        error = null;
        try
        {
            busy = true;
            var outcome = await Commands.SendAsync(
                new SendBidPackageInvite(BidPackageId, composeSubject.Trim(), composeBody,
                    composeTo.Trim(), composeCc.Trim(), composeBcc.Trim()), CancellationToken.None);
            package = outcome.Package;
            draftWebLink = outcome.WebLink;

            if (outcome.Sent)
            {
                showComposeModal = false;
                composeDraftSavedAt = null;
                sendNote = $"Invite sent from the projects mailbox to {outcome.RecipientCount} recipient{(outcome.RecipientCount == 1 ? "" : "s")}, tagged {package.Reference} — replies land under Tender responses.";
                if (outcome.LinkedFiles is { Count: > 0 } linked)
                {
                    sendNote += linked.Count == 1
                        ? $" 1 file was too large to attach and travels as a 7-day download link: {linked[0]}."
                        : $" {linked.Count} files were too large to attach and travel as 7-day download links: {string.Join(", ", linked)}.";
                }
                // The sent copy appears in the Emails tab as the mailbox catches up.
                _ = ReloadEmailsAsync();
            }
            else
            {
                // Staged but not sent — the email survives in Drafts; say so where the user is.
                sendNote = outcome.FailureNote ?? "The send didn't go through — the invite is saved as a draft in the projects mailbox.";
            }
        }
        catch (CommandFailedException ex) { error = $"Couldn't send the invite: {ex.Message}"; }
        catch { error = "Couldn't send the invite. Check the recipients and the mailbox connection, then try again."; }
        finally { busy = false; }
    }

}
