using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Components;

public partial class ManualWorkOrderModal : IDisposable
{
    [Inject] private AiTaskState AiTasks { get; set; } = default!;
    [Inject] private ChatPanelState Chat { get; set; } = default!;

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public string ProjectId { get; set; } = "";

    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>Raised after a successful save so hosts can refresh their stores.</summary>
    [Parameter] public EventCallback OnSaved { get; set; }

    /// <summary>Raised (after OnSaved) when releasing an order triggered the automatic
    /// purchase-order email — the note says what happened so the host can show it.</summary>
    [Parameter] public EventCallback<string> OnPoEmailNote { get; set; }

    /// <summary>The order being edited, with its lines — null when raising a new order.
    /// Manual orders for the whole team; awarded/variation/seeded orders open here too for the
    /// MD, FD and administrators (the API enforces the split and refuses everyone else).</summary>
    [Parameter] public ProjectWorkOrderDetail? Editing { get; set; }

    private WorkOrderForm? form;
    private bool IsEditing => Editing is not null;
    private string EditingReference => Editing?.Order.Reference ?? "";
    private bool FormSaveAsDraft => form?.SaveAsDraft ?? false;
    private decimal OrderTotal => form?.OrderTotal ?? 0m;

    private bool createPackage = true;
    private string packageName = "";
    private string lineSearch = "";
    // Sales line id → this package's £ share, as typed (invariant decimal text).
    private readonly Dictionary<string, string> pickedAmounts = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<ReconciliationPackage> allPackages = Array.Empty<ReconciliationPackage>();

    private bool seeded;
    private bool busy;
    private string? saveError;
    // Set once the order itself has saved, so a package failure can be retried
    // without raising a duplicate order.
    private WorkOrder? createdOrder;
    // Set once the purchase-order email has been handed to the server, so a later
    // retry through this form can never email the supplier twice.
    private bool poEmailAttempted;

    // ---- The no-matching-sale guardrail (raising only). A line's cost centre with no priced
    // valuation report line means committing cost with no sale to claim against — the dialog
    // asks for explicit confirmation, and the acknowledged command has the server record the
    // override in the audit trail. ----
    private bool saleWarningOpen;
    // The centres the user has confirmed the warning FOR — a failed create leaves the form
    // editable, so the gate re-checks each attempt's centres against this set rather than
    // trusting a one-off tick that might predate a swap to a different uncovered centre.
    private readonly HashSet<string> acknowledgedUncoveredCentres = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<string> uncoveredCostCentres = Array.Empty<string>();

    // ---- Attachments (record keeping only). Files picked here are STAGED and only uploaded once
    // the order exists (create) or the edit has saved. ----
    private readonly List<IBrowserFile> stagedAttachmentFiles = new();
    private List<WorkOrderAttachment> existingAttachments = new();
    private string? attachmentNote;
    private string? attachmentError;

    // ---- Files from the assistant chat (assistant-opened dialogs only). The quotes the user
    // attached to the conversation the order is being drafted from, offered as ticks so they can
    // be KEPT on the order without re-picking them from disk. The bytes copy server-side on save.
    // Ticks default to the documents (a quote, a SoW) and leave images (usually pasted
    // screenshots of the tracker) unticked. Keyed by attachment id; each remembers which
    // conversation it lives on, because a task's files split across the handover conversation
    // (attached before "draft this order") and the task's own (attached mid-task). ----
    private sealed record ChatFileRow(AiConversationAttachment File, string ConversationId);
    private List<ChatFileRow> chatFiles = new();
    private readonly HashSet<string> tickedChatFiles = new(StringComparer.OrdinalIgnoreCase);
    private bool chatFilesLoaded;
    // The last AssistantBusy seen — a true→false edge means a turn just ended, and the user may
    // have attached another file mid-conversation, so the list refetches.
    private bool chatAssistantWasBusy;

    // ---- The dialog ⇄ assistant pipe (work_order_edit / work_order_create) ----------------------

    private bool AssistantTaskActive =>
        AiTasks.Active?.ModalKey == ModalCatalog.WorkOrderEdit.ModalKey
        || AiTasks.Active?.ModalKey == ModalCatalog.WorkOrderCreate.ModalKey;

    // The form's opening state has been published to the task once for this opening — so the
    // model's first read sees the order as the dialog pre-filled it, not "{}".
    private bool initialDraftPublished;

    protected override void OnInitialized()
    {
        // The assistant's proposals, when a task is in force. Subscribed for the component's life —
        // the handler itself checks the task matches, so another dialog's task never writes here.
        AiTasks.OnDraftApplied += HandleAssistantDraft;
        // Repaints the working banner as the assistant's turn starts and finishes — and, on a
        // turn ENDING, refetches the chat-file list in case the user attached another quote
        // mid-conversation.
        Chat.OnChange += HandleChatChanged;
    }

    private void HandleChatChanged()
    {
        var turnJustEnded = chatAssistantWasBusy && !Chat.AssistantBusy;
        chatAssistantWasBusy = Chat.AssistantBusy;
        if (turnJustEnded && IsOpen && AssistantTaskActive) _ = ReloadChatFilesAsync();
        StateHasChanged();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        // The Modal renders children only while open, so the form exists (and has seeded itself
        // from the order) only after the first open render — publish its state to the task then.
        if (IsOpen && AssistantTaskActive && !initialDraftPublished && form is not null)
        {
            initialDraftPublished = true;
            AiTasks.UpdateDraft(form.SerialiseState());
        }
    }

    /// <summary>The model's update_open_modal, landing in the form. Merge, republish, repaint.</summary>
    private void HandleAssistantDraft(string fieldsJson)
    {
        if (!IsOpen || !AssistantTaskActive || form is null) return;
        form.ApplyAssistant(fieldsJson);
        AiTasks.UpdateDraft(form.SerialiseState());
        StateHasChanged();
    }

    /// <summary>Every human edit republishes the live state, so the model reasons from the form as
    /// it stands NOW — the same merge-never-replace contract as the other dialogs.</summary>
    private void HandleFormChanged()
    {
        if (AssistantTaskActive && form is not null) AiTasks.UpdateDraft(form.SerialiseState());
        StateHasChanged();
    }

    public void Dispose()
    {
        AiTasks.OnDraftApplied -= HandleAssistantDraft;
        Chat.OnChange -= HandleChatChanged;
    }

    /// <summary>
    /// The files attached to the chat the order is being drafted from: the task's own
    /// conversation plus the handover one (where anything attached BEFORE "draft this order"
    /// lives), oldest first, deduped by id. Fresh ticks default to documents only — an image here
    /// is usually a pasted screenshot of the tracker, not a record worth keeping on the order —
    /// while existing ticks survive a refetch. Failure costs the list, never the dialog: the
    /// order still saves with the files picked from disk.
    /// </summary>
    private async Task ReloadChatFilesAsync()
    {
        if (!AssistantTaskActive) return;
        var conversationIds = new[] { Chat.TaskHandoverConversationId, Chat.ActiveConversationId }
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct()
            .ToList();

        var rows = new List<ChatFileRow>();
        foreach (var conversationId in conversationIds)
        {
            try
            {
                var files = await Queries.AskAsync(
                    new ListAiConversationAttachments(conversationId), CancellationToken.None);
                rows.AddRange(files.Select(file => new ChatFileRow(file, conversationId)));
            }
            catch
            {
                // A list that can't load is absent, not fatal — same trade as existing attachments.
            }
        }

        // One row per FILE, not per register entry: the handover copies the previous chat's
        // attachment rows onto the task's own conversation, so the same quote can answer from
        // both ids. The task conversation's copy wins (it is the one the model reads), and ticks
        // key on name+size so a refetch that swaps which copy is shown cannot lose a tick.
        chatFiles = rows
            .GroupBy(row => TickKey(row.File))
            .Select(group => group
                .OrderByDescending(row => string.Equals(row.ConversationId, Chat.ActiveConversationId, StringComparison.Ordinal))
                .First())
            .OrderBy(row => row.File.UploadedAt)
            .ToList();

        if (!chatFilesLoaded)
        {
            chatFilesLoaded = true;
            foreach (var row in chatFiles.Where(row => !row.File.IsImage))
                tickedChatFiles.Add(TickKey(row.File));
        }
        // Ticks for files that vanished from the conversation must not ride into the save.
        tickedChatFiles.RemoveWhere(key => chatFiles.All(row => !string.Equals(TickKey(row.File), key, StringComparison.Ordinal)));
        StateHasChanged();
    }

    private static string TickKey(AiConversationAttachment file) =>
        $"{file.FileName.ToLowerInvariant()}|{file.SizeBytes}";

    private void ToggleChatFile(ChatFileRow row)
    {
        var key = TickKey(row.File);
        if (!tickedChatFiles.Remove(key)) tickedChatFiles.Add(key);
    }

    /// <summary>
    /// Copies the ticked chat files onto the order, one command per source conversation (the
    /// server checks each conversation is the caller's own). Successes untick and fold into the
    /// existing-attachments list; a failure leaves its ticks in place and returns the server's
    /// sentence, so save-again only re-sends what missed — the same shape as the staged uploads.
    /// </summary>
    private async Task<string?> CopyTickedChatFilesAsync(string workOrderId)
    {
        if (tickedChatFiles.Count == 0) return null;
        var failures = new List<string>();
        foreach (var group in chatFiles
                     .Where(row => tickedChatFiles.Contains(TickKey(row.File)))
                     .GroupBy(row => row.ConversationId))
        {
            var picked = group.ToList();
            try
            {
                existingAttachments = (await WorkOrderAttachments.AttachFromChatAsync(
                    workOrderId, group.Key, picked.Select(row => row.File.AttachmentId).ToList())).ToList();
                foreach (var row in picked) tickedChatFiles.Remove(TickKey(row.File));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failures.Add(ex.Message);
            }
        }
        if (failures.Count == 0) return null;
        return "The chat file(s) couldn't be copied onto the order: " + string.Join(" ", failures)
            + " — the order itself saved; add the file(s) from its PO page.";
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen)
        {
            seeded = false;
            initialDraftPublished = false;
            return;
        }
        if (seeded) return;
        seeded = true;
        // The Modal renders children only while open, so each opening mounts a fresh
        // WorkOrderForm which seeds and freshens its own pickers; this resets the modal's half.
        createPackage = !IsEditing;
        packageName = "";
        lineSearch = "";
        pickedAmounts.Clear();
        saveError = null;
        createdOrder = null;
        poEmailAttempted = false;
        saleWarningOpen = false;
        acknowledgedUncoveredCentres.Clear();
        uncoveredCostCentres = Array.Empty<string>();
        stagedAttachmentFiles.Clear();
        existingAttachments = new List<WorkOrderAttachment>();
        attachmentNote = null;
        attachmentError = null;
        chatFiles = new List<ChatFileRow>();
        tickedChatFiles.Clear();
        chatFilesLoaded = false;
        // Fire-and-forget on purpose: the list arriving late costs nothing (the section simply
        // appears), while awaiting it would hold the whole dialog's seeding on a network call.
        if (AssistantTaskActive) _ = ReloadChatFilesAsync();
        if (IsEditing)
        {
            await LoadExistingAttachmentsAsync();
            return;
        }
        // The packaging step's sales side — only needed when raising.
        var packagesTask = Queries.AskAsync(new ListReconciliationPackagesForProject(ProjectId), CancellationToken.None);
        await Task.WhenAll(
            ValuationLines.RefreshAsync(ProjectId, CancellationToken.None),
            Projects.RefreshAsync(CancellationToken.None));
        allPackages = await packagesTask;
    }

    private static decimal? Parse(string text) => WorkOrderForm.Parse(text);

    // Counting sales lines with how much of each is still available to this new package.
    private List<(ValuationLineItem Line, decimal Available)> FilteredSalesLines
    {
        get
        {
            var taken = allPackages
                .SelectMany(package => package.SalesLines)
                .GroupBy(slice => slice.ValuationLineItemId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Sum(slice => slice.Amount), StringComparer.OrdinalIgnoreCase);
            return ValuationLines.Current(ProjectId)
                .Where(line => line.CountsTowardTotals && line.LineAmount != 0m)
                .Where(line => string.IsNullOrWhiteSpace(lineSearch)
                               || line.Description.Contains(lineSearch.Trim(), StringComparison.OrdinalIgnoreCase)
                               || line.CostCode.Contains(lineSearch.Trim(), StringComparison.OrdinalIgnoreCase)
                               || line.SectionName.Contains(lineSearch.Trim(), StringComparison.OrdinalIgnoreCase)
                               || line.VariationRef.Contains(lineSearch.Trim(), StringComparison.OrdinalIgnoreCase)
                               || line.VariationTitle.Contains(lineSearch.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(line => (Line: line,
                    Available: line.LineAmount - (taken.TryGetValue(line.ValuationLineItemId, out var amount) ? amount : 0m)))
                .OrderBy(entry => entry.Line.CostCode, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Line.DisplayOrder)
                .ToList();
        }
    }

    private void SetSalesAmount(string lineItemId, string? value) => pickedAmounts[lineItemId] = value ?? "";

    private void ToggleSalesLine((ValuationLineItem Line, decimal Available) entry)
    {
        if (pickedAmounts.Remove(entry.Line.ValuationLineItemId)) return;
        // Whole-line default: the full remaining value; edit the amount for a partial share.
        pickedAmounts[entry.Line.ValuationLineItemId] =
            entry.Available.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private decimal SalesTotal => pickedAmounts.Values.Sum(text => Parse(text) ?? 0m);
    private decimal TargetTotal => Math.Round(SalesTotal * FinancialSummaryAssumptions.CostFactor, 2);
    private decimal Difference => TargetTotal - OrderTotal;

    private static string DivisorText =>
        $"{1m + FinancialSummaryAssumptions.MarkupPercent / 100m:0.##}";

    // The packaging half's validation — the core order rules live in WorkOrderForm.
    private string? PackageValidationError
    {
        get
        {
            if (!createPackage || IsEditing || FormSaveAsDraft) return null;
            if (pickedAmounts.Values.Any(text => Parse(text) is not { } amount || amount == 0m))
                return "Every ticked sales line needs a non-zero amount.";
            var linesById = ValuationLines.Current(ProjectId)
                .ToDictionary(line => line.ValuationLineItemId, StringComparer.OrdinalIgnoreCase);
            foreach (var picked in pickedAmounts)
            {
                if (!linesById.TryGetValue(picked.Key, out var salesLine)) continue;
                var amount = Parse(picked.Value)!.Value;
                if (Math.Sign(amount) != Math.Sign(salesLine.LineAmount))
                    return $"\"{Truncate(DescriptionFor(salesLine), 40)}\" — the share must carry the line's sign ({Money(salesLine.LineAmount)}).";
                var available = salesLine.LineAmount - allPackages
                    .SelectMany(package => package.SalesLines)
                    .Where(slice => string.Equals(slice.ValuationLineItemId, picked.Key, StringComparison.OrdinalIgnoreCase))
                    .Sum(slice => slice.Amount);
                if (Math.Abs(amount) > Math.Abs(available))
                    return $"\"{Truncate(DescriptionFor(salesLine), 40)}\" — only {Money(available)} of the line is still available.";
            }
            return null;
        }
    }

    private async Task SaveAsync()
    {
        if (busy || form is null || !form.CanSave) return;
        if (PackageValidationError is not null) return;
        var draft = form.TryBuildDraft();
        if (draft is null) return;
        // The guardrail fires on raising only (drafts included — the commitment is intended the
        // moment it's drafted), and never once the order itself has saved (the package-retry
        // path). Any uncovered centre not yet confirmed reopens the dialog, so editing the
        // lines after a failed attempt can't ride on an earlier acknowledgement.
        if (!IsEditing && createdOrder is null)
        {
            uncoveredCostCentres = FindUncoveredCostCentres(draft.Lines);
            if (uncoveredCostCentres.Any(code => !acknowledgedUncoveredCentres.Contains(code)))
            {
                saleWarningOpen = true;
                return;
            }
        }
        busy = true;
        saveError = null;
        try
        {
            // Editing: the whole editable surface travels in one command — no packaging step.
            if (Editing is not null)
            {
                await Commands.SendAsync(new UpdateManualWorkOrder(
                    ProjectId, Editing.Order.WorkOrderId, draft.SubcontractorId,
                    draft.Title, draft.Scope, form.BuildEditedLines().ToList(),
                    draft.ProgrammeStart, draft.TargetCompletion, draft.ProgrammeNotes,
                    DepositRequired: draft.DepositRequired,
                    DepositPercent: draft.DepositPercent), CancellationToken.None);
                var editAttachmentNote = await UploadStagedAttachmentsAsync(Editing.Order.WorkOrderId);
                var editChatNote = await CopyTickedChatFilesAsync(Editing.Order.WorkOrderId);
                seeded = false; // reseed fresh on next open
                await OnSaved.InvokeAsync();
                var editNote = string.Join(" ", new[] { editAttachmentNote, editChatNote }
                    .Where(note => !string.IsNullOrWhiteSpace(note)));
                if (!string.IsNullOrWhiteSpace(editNote)) await OnPoEmailNote.InvokeAsync(editNote);
                return;
            }

            // Step 1 — the order itself (skipped on a retry after a package failure,
            // so the order is never raised twice).
            createdOrder ??= await Commands.SendAsync(new CreateManualWorkOrder(
                ProjectId, draft.SubcontractorId, draft.Title, draft.Scope,
                Auth.CurrentUser?.Email ?? "", draft.Lines.ToList(),
                draft.ProgrammeStart, draft.TargetCompletion, draft.ProgrammeNotes,
                SaveAsDraft: draft.SaveAsDraft,
                DepositRequired: draft.DepositRequired,
                DepositPercent: draft.DepositPercent,
                UncoveredCostCentresAcknowledged: uncoveredCostCentres.Count > 0), CancellationToken.None);

            // Step 1.5 — the record-keeping attachments, straight onto the fresh order: the
            // staged files from this computer, then the ticked quotes off the assistant chat
            // (copied server-side, store to store).
            attachmentNote = await UploadStagedAttachmentsAsync(createdOrder.WorkOrderId) ?? attachmentNote;
            var chatCopyNote = await CopyTickedChatFilesAsync(createdOrder.WorkOrderId);
            if (chatCopyNote is not null)
                attachmentNote = string.IsNullOrWhiteSpace(attachmentNote) ? chatCopyNote : $"{attachmentNote} {chatCopyNote}";

            // Step 2 — the package, with the fresh order already assigned. Never for a draft:
            // packages carry approved scope, and a draft hasn't been approved yet.
            if (!draft.SaveAsDraft && createPackage && pickedAmounts.Count > 0)
            {
                var slices = pickedAmounts
                    .Where(picked => Parse(picked.Value) is { } amount && amount != 0m)
                    .Select(picked => new PackageSalesSlice(picked.Key, Parse(picked.Value)!.Value))
                    .ToList();
                await Commands.SendAsync(new SaveReconciliationPackage(
                    ProjectId,
                    null,
                    string.IsNullOrWhiteSpace(packageName) ? draft.Title : packageName.Trim(),
                    new List<string> { createdOrder.WorkOrderId },
                    slices), CancellationToken.None);
            }

            // Step 3 — the promised purchase-order email (released orders only; a draft's email
            // waits for approval). Non-fatal by design.
            string? poEmailNote = null;
            if (!draft.SaveAsDraft && !poEmailAttempted)
            {
                var supplier = form.SelectedSupplier;
                if (supplier is null || string.IsNullOrWhiteSpace(supplier.ContactEmail))
                {
                    poEmailNote = $"{createdOrder.Reference} was raised, but the supplier has no email address in the "
                        + "directory so the purchase order wasn't emailed — add one, then send it from the PO page.";
                }
                else
                {
                    poEmailAttempted = true;
                    var projectName = Projects.Find(ProjectId)?.Name ?? "";
                    var emailLines = draft.Lines
                        .Select(line => new WorkOrderPoEmail.Line(line.Title, 1m, "item", line.Amount))
                        .ToList();
                    try
                    {
                        var outcome = await Commands.SendAsync(new SendWorkOrderPoEmail(
                            createdOrder.WorkOrderId,
                            WorkOrderPoEmail.Subject(createdOrder, string.IsNullOrWhiteSpace(projectName) ? ProjectId : projectName),
                            WorkOrderPoEmail.Body(createdOrder, supplier.CompanyName, emailLines, projectName, Nav.BaseUri)),
                            CancellationToken.None);
                        poEmailNote = outcome.Sent
                            ? $"{createdOrder.Reference} was raised and the purchase order was emailed to {outcome.RecipientEmail}."
                            : $"{createdOrder.Reference} was raised. {outcome.FailureNote}";
                    }
                    catch (CommandFailedException ex)
                    {
                        poEmailNote = $"{createdOrder.Reference} was raised, but the purchase-order email couldn't be sent: "
                            + $"{ex.Message} You can send it from the PO page.";
                    }
                    catch
                    {
                        poEmailNote = $"{createdOrder.Reference} was raised, but the purchase-order email couldn't be sent "
                            + "— you can send it from the PO page.";
                    }
                }
            }

            seeded = false; // reseed fresh on next open
            await OnSaved.InvokeAsync();
            // One note channel, one message: the PO-email outcome and any attachment failure
            // travel together.
            var savedNote = string.Join(" ", new[] { poEmailNote, attachmentNote }.Where(note => !string.IsNullOrWhiteSpace(note)));
            if (!string.IsNullOrWhiteSpace(savedNote)) await OnPoEmailNote.InvokeAsync(savedNote);
        }
        catch (CommandFailedException ex)
        {
            saveError = IsEditing
                ? $"Couldn't save the changes: {ex.Message}"
                : createdOrder is null
                ? $"Couldn't raise the order: {ex.Message}"
                : $"The order was raised ({createdOrder.Reference}) but the package couldn't be created: {ex.Message} — fix and save again, or close and build the package from the Packages section.";
        }
        finally
        {
            busy = false;
        }
    }

    private async Task ConfirmSaleWarningAsync()
    {
        foreach (var code in uncoveredCostCentres) acknowledgedUncoveredCentres.Add(code);
        saleWarningOpen = false;
        await SaveAsync();
    }

    private void CancelSaleWarning() => saleWarningOpen = false;

    /// <summary>The order's cost centres with no priced valuation report line — no contract or
    /// variation order sale to set the committed cost against. Declined/TBC lines don't count
    /// as cover (CountsTowardTotals); the server applies the same rule as the final word.</summary>
    private IReadOnlyList<string> FindUncoveredCostCentres(IReadOnlyList<ManualWorkOrderLine> orderLines)
    {
        var pricedCodes = ValuationLines.Current(ProjectId)
            .Where(line => line.CountsTowardTotals)
            .Select(line => line.CostCode);
        var pricedSet = new HashSet<string>(pricedCodes, StringComparer.OrdinalIgnoreCase);
        return orderLines
            .Select(line => line.CostCode)
            .Where(code => !pricedSet.Contains(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void OnAttachmentFilesSelected(InputFileChangeEventArgs e)
    {
        attachmentError = null;
        stagedAttachmentFiles.AddRange(e.GetMultipleFiles(20));
    }

    private async Task LoadExistingAttachmentsAsync()
    {
        if (Editing is null) return;
        try { existingAttachments = (await WorkOrderAttachments.ListAsync(Editing.Order.WorkOrderId)).ToList(); }
        catch { existingAttachments = new List<WorkOrderAttachment>(); }
    }

    private async Task RemoveExistingAttachmentAsync(WorkOrderAttachment attachment)
    {
        if (busy || Editing is null) return;
        attachmentError = null;
        try
        {
            existingAttachments = (await WorkOrderAttachments.RemoveAsync(
                Editing.Order.WorkOrderId, attachment.WorkOrderAttachmentId)).ToList();
        }
        catch (CommandFailedException ex) { attachmentError = ex.Message; }
        catch { attachmentError = "Couldn't remove that attachment. Please try again."; }
    }

    /// <summary>Uploads the staged files one at a time (successes leave the staged list, so a
    /// retry only re-sends what failed). Returns a human note when anything failed, else null.</summary>
    private async Task<string?> UploadStagedAttachmentsAsync(string workOrderId)
    {
        if (stagedAttachmentFiles.Count == 0) return null;
        var failed = new List<string>();
        foreach (var file in stagedAttachmentFiles.ToList())
        {
            try
            {
                await WorkOrderAttachments.UploadFilesAsync(workOrderId, new[] { file });
                stagedAttachmentFiles.Remove(file);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed.Add(file.Name + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $" ({ex.Message})"));
            }
        }
        if (failed.Count == 0) return null;
        return (failed.Count == 1 ? "One attachment" : $"{failed.Count} attachments")
            + " couldn't be stored: " + string.Join("; ", failed)
            + " — the order itself saved; add the file(s) from its PO page.";
    }

    private static string FormatBytes(long bytes) =>
        bytes >= 1_048_576 ? $"{bytes / 1_048_576.0:0.#} MB"
        : bytes >= 1024 ? $"{bytes / 1024.0:0.#} KB"
        : $"{bytes} B";

    // Mirrors the Valuation Report tab's CodeFor so the picker cross-references against that tab.
    private static string Ref(ValuationLineItem line) =>
        line.ElementType == ValuationElementType.Variation
            ? (string.IsNullOrWhiteSpace(line.VariationRef) ? line.CostCode : line.VariationRef)
            : (string.IsNullOrWhiteSpace(line.CostCode) ? line.SectionCode : line.CostCode);

    private static string RefTitle(ValuationLineItem line) =>
        line.ElementType == ValuationElementType.Variation
            ? line.VariationTitle
            : string.IsNullOrWhiteSpace(line.SectionCode)
                ? line.SectionName
                : $"{line.SectionCode} — {line.SectionName}";

    // Variation lines mirror an approved VO whose descriptive text lives in VariationTitle.
    private static string DescriptionFor(ValuationLineItem line) =>
        line.ElementType == ValuationElementType.Variation && string.IsNullOrWhiteSpace(line.Description)
            ? line.VariationTitle
            : line.Description;

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    private static string Money(decimal value) => WorkOrderForm.Money(value);
}
