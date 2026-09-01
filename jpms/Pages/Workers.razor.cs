using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Pages;

public partial class Workers
{
    private const decimal StandardDayHours = 8m; // scope §4: standard day; per-worker override deferred

    // Session checked and the user signed in. This is NOT "the data is here" — keeping the two
    // apart is what lets the page show its chrome at once and hold the registry until it lands.

    // A failed fetch has to open the gate, or the jewel pulses forever; the table then says so.
    private bool workersFailed;
    private string? actionError;

    // Everything the registry table reads: the workers themselves and the directory the
    // Subcontractor column is resolved against.
    private bool RegistryReady => Labour.WorkersLoaded && Subcontractors.IsLoaded;

    private string? editingWorkerId;
    private string? deletingWorkerId;
    private string formName = "";
    private decimal formDayRate;
    private string formSubcontractorId = "";
    private string formPhone = "";
    private bool formIsActive = true;
    private string formEmail = "";
    private bool formIsSoleTrader;
    private DateTime? formEngagedFrom;
    private DateTime? formEngagedTo;

    private IReadOnlyList<SearchSelect.Option> DirectoryOptions =>
        Subcontractors.All().Where(s => !s.IsProspect)
            .OrderBy(s => s.CompanyName)
            .Select(s => new SearchSelect.Option(s.SubcontractorId, s.CompanyName))
            .ToList();

    // The form lives in a modal now. Its own failures stay inside it (formError) — a message shown
    // on the page behind a dialog is a message nobody reads. Failures from the table's own actions
    // (delete) still use actionError, which renders above the table where they happened.
    private bool isFormOpen;
    private bool isSaving;
    private string? formError;

    private string FormTitle => editingWorkerId is null ? "Add worker" : "Edit worker";
    private string FormConfirmLabel => editingWorkerId is null ? "Add worker" : "Save changes";

    // ModalNotes — why the Modal above is written the way it is. Kept here, in C#, because prose
    // about Razor syntax cannot safely be written in a Razor comment (see the third note).
    //
    // 1. Title and ConfirmLabel come from these properties rather than inline ternaries. A nested
    //    double-quoted string inside an @(...) attribute value is a shape the Razor parser misreads.
    //
    // 2. ConfirmDisabled binds ONLY to isSaving. Disabling the button on an incomplete form
    //    deadlocks the dialog: bindings update on blur, a disabled button takes no click and so
    //    never blurs the field being typed in, and the last value typed never reaches the model.
    //    The user fills the form correctly, presses the button, and nothing happens, for ever.
    //    SaveAsync validates instead and names the field that is wrong.
    //
    // 3. Razor comments are only valid BETWEEN elements or inside element content — never between
    //    a tag's attributes, where the comment is parsed as an attribute name and Blazor throws at
    //    render. They also do not nest: the first closing delimiter ends the comment, so a comment
    //    whose prose quotes those delimiters spills its remaining text out as C# and fails to
    //    build. Both mistakes were made here on 2026-07-26; hence this note lives in C#.

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        Labour.OnChange += StateHasChanged;
        // The directory feeds the picker and the Subcontractor column, so its arrival has to
        // re-render this page too — nothing else here listens for it.
        Subcontractors.OnChange += StateHasChanged;
        // Paint the chrome before the fetch: Blazor re-renders OnInitializedAsync only at its
        // FIRST await, which has already passed, so without this the page waits on the registry.
        StateHasChanged();
        // The directory is lazily loaded: All() is what starts its fetch, and every call to it sits
        // inside the registry gate. Without this the gate waits on a load its own closure prevented
        // from ever starting — the workers arrive, IsLoaded stays false, and the jewel pulses
        // forever. Any gate that waits on a lazily-loaded store has to start that store itself.
        Subcontractors.All();
        try { await Labour.RefreshWorkersAsync(); }
        catch { workersFailed = true; }
    }

    public void Dispose()
    {
        Labour.OnChange -= StateHasChanged;
        Subcontractors.OnChange -= StateHasChanged;
    }

    private decimal HourlyRate() => StandardDayHours == 0m ? 0m : Math.Round(formDayRate / StandardDayHours, 2);

    private ExcelWorkbook? BuildExportWorkbook(bool _)
    {
        var workers = Labour.Workers();
        if (workers.Count == 0) return null;

        var workbook = new ExcelWorkbook();
        var sheet = workbook.AddSheet("Workers",
            new ExcelColumn("Name"),
            new ExcelColumn("Portal email"),
            new ExcelColumn("Subcontractor"),
            new ExcelColumn("Hourly £", ExcelFormat.Currency),
            new ExcelColumn("Day £ 8h", ExcelFormat.Currency),
            new ExcelColumn("Status"));

        foreach (var worker in workers)
        {
            sheet.AddRow(
                worker.Name,
                worker.ContactEmail,
                // SubcontractorName() dashes a missing link for the screen; the export leaves it
                // as a blank cell instead (nulls stay null — no "—" substitution).
                worker.SubcontractorId is null ? null
                    : Subcontractors.All().FirstOrDefault(s => s.SubcontractorId == worker.SubcontractorId)?.CompanyName,
                worker.HourlyRate,
                worker.HourlyRate * 8m,
                worker.IsActive ? "Active" : "Inactive");
        }
        return workbook;
    }

    private string SubcontractorName(string? subcontractorId) =>
        subcontractorId is null ? "—"
        : Subcontractors.All().FirstOrDefault(subcontractor => subcontractor.SubcontractorId == subcontractorId)?.CompanyName ?? "—";

    private async Task ConfirmDeleteAsync(string workerId)
    {
        actionError = null;
        deletingWorkerId = null;
        try
        {
            await Labour.DeleteWorkerAsync(workerId);
        }
        catch (CommandFailedException failure)
        {
            // e.g. the worker has timesheet/register history and can only be deactivated.
            actionError = failure.Message;
        }
        catch (Exception) { actionError = "Could not delete the worker — try again."; }
    }

    private void StartAdd()
    {
        ClearForm();
        // A delete failure from before, and a half-confirmed delete row, are both stale the moment
        // the dialog covers them.
        actionError = null;
        deletingWorkerId = null;
        isFormOpen = true;
    }

    private void StartEdit(Worker worker)
    {
        ClearForm();
        editingWorkerId = worker.WorkerId;
        formName = worker.Name;
        formDayRate = worker.HourlyRate * StandardDayHours;
        formSubcontractorId = worker.SubcontractorId ?? "";
        formPhone = worker.ContactPhone;
        formEmail = worker.ContactEmail;
        formIsActive = worker.IsActive;
        formIsSoleTrader = worker.IsSoleTrader;
        formEngagedFrom = worker.EngagedFrom?.UtcDateTime.Date;
        formEngagedTo = worker.EngagedTo?.UtcDateTime.Date;
        // A half-finished delete confirmation on another row is stale the moment a dialog covers it.
        deletingWorkerId = null;
        isFormOpen = true;
    }

    /// <summary>
    /// The user asking to close: backdrop, ✕, Cancel. Modal wires all three to OnCancel and never
    /// closes itself, so this is the only route in. Refused mid-save — clearing the form the
    /// in-flight request is still using would land the rejection on a dialog nobody can see, and a
    /// duplicate email would vanish silently along with the typing (400/409/422 raise no toast by
    /// design). The button is disabled while saving, so the wait is visible rather than mysterious.
    /// </summary>
    private void CloseForm()
    {
        if (isSaving) return;
        DismissForm();
    }

    /// <summary>
    /// Closing because the work is done. Kept separate from CloseForm: SaveAsync succeeds while
    /// isSaving is still true, so routing it through the guard above would swallow the close and
    /// leave the dialog open over a saved worker — inviting the user to press Add a second time.
    /// </summary>
    private void DismissForm()
    {
        isFormOpen = false;
        ClearForm();
    }

    private void ClearForm()
    {
        editingWorkerId = null;
        formName = "";
        formDayRate = 0m;
        formSubcontractorId = "";
        formPhone = "";
        formEmail = "";
        formIsActive = true;
        formIsSoleTrader = false;
        formEngagedFrom = null;
        formEngagedTo = null;
        formError = null;
    }

    // Date inputs bind DateTime (Kind Unspecified); the contract carries DateTimeOffset. Year/
    // month/day only — never the DateTime itself with a Local kind (the BST lesson).
    private static DateTimeOffset? AsOffset(DateTime? date) =>
        date is { } value ? new DateTimeOffset(value.Date, TimeSpan.Zero) : null;

    // ---- Directory matching card (2026-08-31) --------------------------------------------------

    private WorkerDirectoryLinkReport? linkReport;
    private bool isMatching;
    private string? matchError;

    private IReadOnlyList<Worker> UnlinkedWorkers =>
        Labour.Workers().Where(worker => worker.IsActive && worker.SubcontractorId is null && !worker.IsSoleTrader).ToList();

    private async Task FindMatchesAsync()
    {
        isMatching = true; matchError = null;
        try { linkReport = await Labour.ReconcileWorkerLinksAsync(apply: false); }
        catch (CommandFailedException failure) { matchError = failure.Message; }
        catch (Exception) { matchError = "Could not run the matching — try again."; }
        finally { isMatching = false; }
    }

    private async Task ApplyAllMatchesAsync()
    {
        isMatching = true; matchError = null;
        try
        {
            await Labour.ReconcileWorkerLinksAsync(apply: true);
            // Re-run the dry run so the card shows exactly what remains for a human decision.
            linkReport = await Labour.ReconcileWorkerLinksAsync(apply: false);
        }
        catch (CommandFailedException failure) { matchError = failure.Message; }
        catch (Exception) { matchError = "Could not apply the matches — try again."; }
        finally { isMatching = false; }
    }

    private async Task ApplyLinkAsync(string workerId, string subcontractorId)
    {
        isMatching = true; matchError = null;
        try
        {
            await Labour.SetWorkerSettlementIdentityAsync(workerId, subcontractorId, isSoleTrader: false);
            linkReport = await Labour.ReconcileWorkerLinksAsync(apply: false);
        }
        catch (CommandFailedException failure) { matchError = failure.Message; }
        catch (Exception) { matchError = "Could not link the worker — try again."; }
        finally { isMatching = false; }
    }

    private async Task MarkSoleTraderAsync(string workerId)
    {
        isMatching = true; matchError = null;
        try
        {
            await Labour.SetWorkerSettlementIdentityAsync(workerId, subcontractorId: null, isSoleTrader: true);
            linkReport = await Labour.ReconcileWorkerLinksAsync(apply: false);
        }
        catch (CommandFailedException failure) { matchError = failure.Message; }
        catch (Exception) { matchError = "Could not flag the worker — try again."; }
        finally { isMatching = false; }
    }

    private async Task SaveAsync()
    {
        formError = null;
        if (string.IsNullOrWhiteSpace(formName)) { formError = "A name is required."; return; }
        if (formDayRate <= 0m) { formError = "A day rate above zero is required."; return; }

        var subcontractorId = formSubcontractorId == "" ? null : formSubcontractorId;
        if (formEngagedFrom is { } from && formEngagedTo is { } to && to < from)
        { formError = "Engaged to cannot be before engaged from."; return; }
        // The company link always wins — a linked worker saved with the flag would carry a
        // dormant flag the settlement layer ignores; keep the record honest instead.
        var isSoleTrader = subcontractorId is null && formIsSoleTrader;
        isSaving = true;
        try
        {
            if (editingWorkerId is null)
                await Labour.AddWorkerAsync(formName, HourlyRate(), subcontractorId, formEmail, formPhone,
                    isSoleTrader, AsOffset(formEngagedFrom), AsOffset(formEngagedTo));
            else
                await Labour.UpdateWorkerAsync(new Worker(editingWorkerId, formName, subcontractorId, HourlyRate(),
                    formIsActive, formEmail, formPhone, isSoleTrader, AsOffset(formEngagedFrom), AsOffset(formEngagedTo)));
            DismissForm();
        }
        catch (CommandFailedException rejection)
        {
            // The endpoint's own words — a duplicate email, a rate the gate refused, a 403 from a
            // role that cannot manage the registry. Keep the dialog open on top of the filled-in
            // form: closing it would throw the user's typing away along with the explanation.
            formError = rejection.Message;
        }
        catch (Exception)
        {
            formError = "Could not save the worker — check your connection and try again.";
        }
        finally { isSaving = false; }
    }
}
