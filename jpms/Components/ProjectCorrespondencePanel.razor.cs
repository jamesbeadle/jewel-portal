using static Jewel.JPMS.MoneyFormats;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Components;

public partial class ProjectCorrespondencePanel
{
    [Parameter, EditorRequired] public string ProjectId { get; set; } = "";

    private static readonly ProjectContactRole[] RoleOptions =
    {
        ProjectContactRole.Client, ProjectContactRole.Architect, ProjectContactRole.Consultant,
        ProjectContactRole.Engineer, ProjectContactRole.Contractor, ProjectContactRole.Other
    };

    private bool loading = true;
    private bool busy;
    private string? error;
    private string? partyName;
    private IReadOnlyList<PartyContact> partyContacts = Array.Empty<PartyContact>();
    private IReadOnlyList<ProjectContact> profileRows = Array.Empty<ProjectContact>();

    private string? editingContactId;
    private string formName = "";
    private string formEmail = "";
    private string formOrganisation = "";
    private int formRole = (int)ProjectContactRole.Other;
    private int formRouting = (int)CorrespondenceRouting.None;

    private Project? Project => Projects.Find(ProjectId);

    // Mirrors ProjectContactAuthorisation server-side.
    private bool CanManage =>
        Session.AvailableRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector or Role.ProjectManager or Role.SiteManager);

    private IReadOnlyList<ProjectContact> adHocContacts =>
        profileRows.Where(row => row.PartyContactId is null).ToList();

    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private async Task ReloadAsync()
    {
        loading = true;
        error = null;
        try
        {
            profileRows = await Correspondence.ListProjectContactsAsync(ProjectId);

            var project = Project;
            if (project is not null && !string.IsNullOrEmpty(project.PartyId))
            {
                partyContacts = await Correspondence.ListPartyContactsAsync(project.PartyKind, project.PartyId);
                partyName = project.PartyKind == PartyKind.Architect
                    ? (await ArchitectStore.GetAsync(project.PartyId))?.Name
                    : (await ClientStore.GetAsync(project.PartyId))?.Name;
            }
            else
            {
                partyContacts = Array.Empty<PartyContact>();
                partyName = null;
            }
        }
        catch { error = "Couldn't load the correspondence profile. Please try again."; }
        finally { loading = false; StateHasChanged(); }
    }

    private ProjectContact? OverrideFor(string partyContactId) =>
        profileRows.FirstOrDefault(row => row.PartyContactId == partyContactId);

    private CorrespondenceRouting EffectiveRouting(PartyContact contact) =>
        OverrideFor(contact.PartyContactId)?.Routing ?? contact.DefaultRouting;

    // Re-routing a party contact for this project: an override row is written when the choice
    // differs from the contact's default, and removed when set back to it (restoring inheritance).
    private async Task SetPartyContactRoutingAsync(PartyContact contact, ChangeEventArgs e)
    {
        if (busy || !int.TryParse(e.Value?.ToString(), out var value)) return;
        var routing = (CorrespondenceRouting)value;
        var existing = OverrideFor(contact.PartyContactId);

        await MutateAsync(async () =>
        {
            if (routing == contact.DefaultRouting)
            {
                if (existing is not null)
                    await Correspondence.RemoveProjectContactAsync(ProjectId, existing.ContactId);
                return;
            }

            await Correspondence.UpsertProjectContactAsync(new UpsertProjectContact(
                ProjectId,
                contact.Name,
                contact.Email,
                Project?.PartyKind == PartyKind.Architect ? ProjectContactRole.Architect : ProjectContactRole.Client,
                ReceivesRequests: routing == CorrespondenceRouting.To,
                Organisation: partyName,
                ContactId: existing?.ContactId,
                Routing: routing,
                PartyContactId: contact.PartyContactId));
        });
    }

    private async Task SetAdHocRoutingAsync(ProjectContact contact, ChangeEventArgs e)
    {
        if (busy || !int.TryParse(e.Value?.ToString(), out var value)) return;
        var routing = (CorrespondenceRouting)value;
        await MutateAsync(() => Correspondence.UpsertProjectContactAsync(new UpsertProjectContact(
            ProjectId, contact.Name, contact.Email, contact.Role,
            ReceivesRequests: routing == CorrespondenceRouting.To,
            Organisation: contact.Organisation,
            ContactId: contact.ContactId,
            Routing: routing)));
    }

    private void BeginEdit(ProjectContact contact)
    {
        editingContactId = contact.ContactId;
        formName = contact.Name;
        formEmail = contact.Email;
        formOrganisation = contact.Organisation ?? "";
        formRole = (int)contact.Role;
        formRouting = (int)contact.Routing;
        error = null;
    }

    private void CancelEdit()
    {
        editingContactId = null;
        ResetForm();
    }

    private void ResetForm()
    {
        formName = formEmail = formOrganisation = "";
        formRole = (int)ProjectContactRole.Other;
        formRouting = (int)CorrespondenceRouting.None;
    }

    private async Task SaveContactAsync()
    {
        if (busy) return;
        if (string.IsNullOrWhiteSpace(formName) || string.IsNullOrWhiteSpace(formEmail))
        {
            error = "A name and email are required.";
            return;
        }
        var routing = (CorrespondenceRouting)formRouting;
        await MutateAsync(() => Correspondence.UpsertProjectContactAsync(new UpsertProjectContact(
            ProjectId, formName.Trim(), formEmail.Trim(), (ProjectContactRole)formRole,
            ReceivesRequests: routing == CorrespondenceRouting.To,
            Organisation: string.IsNullOrWhiteSpace(formOrganisation) ? null : formOrganisation.Trim(),
            ContactId: editingContactId,
            Routing: routing)));
        if (error is null)
        {
            editingContactId = null;
            ResetForm();
        }
    }

    private async Task RemoveAdHocAsync(ProjectContact contact)
    {
        if (contact.ContactId == editingContactId) CancelEdit();
        await MutateAsync(() => Correspondence.RemoveProjectContactAsync(ProjectId, contact.ContactId));
    }

    private async Task MutateAsync(Func<Task> action)
    {
        error = null;
        try
        {
            busy = true;
            await action();
            await ReloadAsync();
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "Couldn't save the change. Please try again."; }
        finally { busy = false; }
    }
}
