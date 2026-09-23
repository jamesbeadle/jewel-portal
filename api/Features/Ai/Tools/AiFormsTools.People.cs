using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiFormsTools
{
    private static AiTool FoldersTool() => new(
        "list_form_folders",
        "The people and companies forms are filed under: formFolderId, the name, Person or Company, the Jewel company, how many "
        + "forms and the last one, and the two dates that start the destruction clocks — the day an engagement ended and the day "
        + "a company vehicle came back. record_form_folder_dates writes them.",
        AiToolSchema.Empty(),
        AiToolKind.Read,
        FormRoleSets.Office,
        FoldersAsync);

    private static async Task<string> FoldersAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var folders = await Query<ListFormFolders, IReadOnlyList<FormFolder>>(context, new ListFormFolders(), ct);
        return Serialise(new { ok = true, folders });
    }

    private static AiTool LicenceChecksTool() => new(
        "list_driving_licence_checks",
        "The licence checks recorded on Company Vehicle Forms: the form, the day the DVLA record was viewed, whether the driver "
        + "meets the insurance criteria, the note and who checked. Only the outcome is kept; record_driving_licence_check adds one.",
        AiToolSchema.Empty(),
        AiToolKind.Read,
        FormRoleSets.Office,
        LicenceChecksAsync);

    private static async Task<string> LicenceChecksAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var checks = await Query<ListDrivingLicenceChecks, IReadOnlyList<DrivingLicenceCheck>>(context, new ListDrivingLicenceChecks(), ct);
        return Serialise(new { ok = true, checks });
    }

    private static AiTool EmergencyContactsTool() => new(
        "list_emergency_contacts",
        "Each person's latest emergency contact — who to ring, their relationship, phone, email and address — for whoever is on "
        + "site when something happens. hasHealthAnswers says the person gave health information; it is revealed on the "
        + "Emergency contacts page by someone who may see it, each look on the audit trail, and never over the connector.",
        AiToolSchema.Empty(),
        AiToolKind.Read,
        FormRoleSets.EmergencyContactReaders,
        EmergencyContactsAsync);

    private static async Task<string> EmergencyContactsAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var contacts = await Query<ListEmergencyContacts, IReadOnlyList<EmergencyContactCard>>(context, new ListEmergencyContacts(), ct);
        return Serialise(new { ok = true, contacts });
    }
}
