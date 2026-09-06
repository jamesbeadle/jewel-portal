using Jewel.JPMS.Contracts.ArchitectInstructions;

namespace Jewel.JPMS.Pages;

public partial class ProjectArchitectInstructions
{
    [Parameter] public string ProjectId { get; set; } = "";

    // Session checked and the user is signed in — the heading and the File button show straight
    // away. Whether the register has arrived is a separate question, answered by `instructions`.
    private bool busy;
    private string? error;
    private string? dialogError;

    // Nullable on purpose: an empty register is a real answer, so "not fetched yet" has to be a
    // distinct state — otherwise the panel says "none recorded" before it has looked.
    private List<ArchitectInstruction>? instructions;
    private List<VariationOrder> variations = new();

    private bool fileDialogOpen;
    private ArchitectInstruction? linking;
    private ArchitectInstruction? confirmingDelete;

    private string formRef = "";
    private string formTitle = "";
    private string formNotes = "";
    private string formInstructedAt = "";
    private string formIssuedBy = "";
    private IBrowserFile? formFile;
    private readonly HashSet<string> formVariationIds = new(StringComparer.OrdinalIgnoreCase);

    // Filing, correcting and linking mirror the API's ArchitectInstructionRoles.AllowedToManage —
    // the architect issues these, so the architect can file them without going through Jewel.
    private bool CanManage => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector
             or Role.ProjectManager or Role.Architect);

    // Variations waiting on an instruction come first — they are the reason someone is on this page.
    private List<VariationOrder> VariationOptions =>
        variations
            .OrderByDescending(variation => variation.Status == VariationOrderStatus.AwaitingArchitectInstruction)
            .ThenByDescending(variation => variation.Number)
            .ToList();

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        if (!Session.IsApproved) return;

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        error = null;
        try
        {
            instructions = (await Instructions.ListAsync(ProjectId)).ToList();
        }
        catch
        {
            error = "Couldn't load the instruction register. Please try again.";
        }

        try
        {
            variations = (await Variations.ListForProjectAsync(ProjectId)).ToList();
        }
        catch
        {
            // The register still reads without the variation list; only the link pickers degrade.
            variations = new List<VariationOrder>();
        }
    }

    private void OpenFileDialog()
    {
        dialogError = null;
        formRef = formTitle = formNotes = formInstructedAt = "";
        formIssuedBy = "";
        formFile = null;
        formVariationIds.Clear();
        fileDialogOpen = true;
    }

    private void OnInstructionFileSelected(InputFileChangeEventArgs e) => formFile = e.File;

    private void ToggleVariation(string variationOrderId, bool selected)
    {
        if (selected) formVariationIds.Add(variationOrderId);
        else formVariationIds.Remove(variationOrderId);
    }

    private async Task SubmitInstruction()
    {
        if (busy) return;
        dialogError = null;

        if (string.IsNullOrWhiteSpace(formTitle) && string.IsNullOrWhiteSpace(formRef))
        {
            dialogError = "Give the instruction a reference or a title so it can be found again.";
            return;
        }

        DateTimeOffset? instructedAt = null;
        if (!string.IsNullOrWhiteSpace(formInstructedAt))
        {
            if (!DateTimeOffset.TryParse(formInstructedAt, out var parsed))
            {
                dialogError = "The instruction date couldn't be read.";
                return;
            }
            instructedAt = parsed;
        }

        try
        {
            busy = true;
            await Instructions.FileAsync(
                ProjectId, formRef.Trim(), formTitle.Trim(),
                string.IsNullOrWhiteSpace(formNotes) ? null : formNotes.Trim(),
                instructedAt,
                string.IsNullOrWhiteSpace(formIssuedBy) ? null : formIssuedBy.Trim(),
                formFile,
                formVariationIds.ToList());
            fileDialogOpen = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            dialogError = ex.Message;
        }
        finally { busy = false; }
    }

    private void OpenLinkDialog(ArchitectInstruction instruction)
    {
        dialogError = null;
        linking = instruction;
    }

    private void OpenDeleteDialog(ArchitectInstruction instruction)
    {
        dialogError = null;
        confirmingDelete = instruction;
    }

    private async Task Link(string variationOrderId)
    {
        if (busy || linking is null) return;
        dialogError = null;
        try
        {
            busy = true;
            var updated = await Instructions.LinkToVariationAsync(linking.ArchitectInstructionId, variationOrderId);
            linking = updated;
            await LoadAsync();
        }
        catch (CommandFailedException ex) { dialogError = ex.Message; }
        catch { dialogError = "Couldn't link that variation. Please try again."; }
        finally { busy = false; }
    }

    private async Task Unlink(string variationOrderId)
    {
        if (busy || linking is null) return;
        dialogError = null;
        try
        {
            busy = true;
            var updated = await Instructions.UnlinkFromVariationAsync(linking.ArchitectInstructionId, variationOrderId);
            linking = updated;
            await LoadAsync();
        }
        catch (CommandFailedException ex) { dialogError = ex.Message; }
        catch { dialogError = "Couldn't unlink that variation. Please try again."; }
        finally { busy = false; }
    }

    // Failures show INSIDE the modal: the page-level banner sits behind the overlay, so writing
    // there would look like the button had done nothing at all.
    private async Task ConfirmDelete()
    {
        if (busy || confirmingDelete is null) return;
        dialogError = null;
        try
        {
            busy = true;
            await Instructions.DeleteAsync(confirmingDelete.ArchitectInstructionId);
            confirmingDelete = null;
            await LoadAsync();
        }
        catch (CommandFailedException ex) { dialogError = ex.Message; }
        catch { dialogError = "Couldn't delete that instruction. Please try again."; }
        finally { busy = false; }
    }

    private static string Date(DateTimeOffset value) => DateText(value);
}
