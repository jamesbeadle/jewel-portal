using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Forms.Filing;

/// <summary>
/// Everything one person or company sends sits together across every form type (api/forms-intake.js
/// fileUploads), under one folder per name, kind and Jewel company — a person engaged by both
/// companies has two engagements and two retention clocks. The folder is created with its first form.
/// </summary>
internal static class FormFolderFiling
{
    private const int LongestName = 256;

    public static async Task<FormFolderEntity> FolderForAsync(
        JpmsContext context, FormDefinition form, JewelCompany company, string filingName, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var name = filingName.Length > LongestName ? filingName[..LongestName] : filingName;
        var normalised = FormFilingNames.Normalised(name);
        var folder = await context.FormFolders.FirstOrDefaultAsync(
            row => row.NormalizedName == normalised && row.Kind == (int)form.FilingKind && row.Company == (int)company,
            cancellationToken);
        if (folder is null)
        {
            folder = NewFolder(name, normalised, form.FilingKind, company, now);
            context.FormFolders.Add(folder);
        }
        folder.LastSubmittedAt = now;
        return folder;
    }

    private static FormFolderEntity NewFolder(string name, string normalised, FormFilingKind kind, JewelCompany company, DateTimeOffset now) => new()
    {
        FormFolderId = FormIdentifierFactory.NextId(),
        Name = name,
        NormalizedName = normalised,
        Kind = (int)kind,
        Company = (int)company,
        CreatedAt = now,
        LastSubmittedAt = now
    };
}
