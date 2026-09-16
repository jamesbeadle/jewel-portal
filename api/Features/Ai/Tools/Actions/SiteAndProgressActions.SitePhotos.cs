using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Api.Features.Progress.SitePhotos;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class SiteAndProgressActions
{
    private const string SitePhotoArea = "Progress & programme";

    private static IEnumerable<AiAction> SitePhotoActions() => new AiAction[]
    {
        new AiAction(
            Name: "file_site_photos",
            Area: SitePhotoArea,
            Description: "Files photographs from the company-wide site photo pool onto ONE progress "
                + "update, after its current photos, in the order given — the write half of the "
                + "fingerprint match (match_site_photos finds the pool photos, this puts them on the "
                + "day). Each photo is copied onto the update as an ordinary progress photo, the "
                + "pool row is stamped with where it went, and it cannot be filed a second time. "
                + "The answer is the update as it now stands and one outcome per photo: filed, "
                + "already on this update, already filed onto another update, not found, or failed "
                + "on its own — a refused photo never stops the others.",
            CommandType: typeof(FileSitePhotos),
            ResultType: typeof(SitePhotoFilingResult),
            AuthorisationType: typeof(FileSitePhotosAuthorisation),
            ValidationType: typeof(FileSitePhotosValidation),
            VisibleTo: ProgressRoles.Contributors,
            EmailStamps: new[] { "FiledByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "progressUpdateId is the day's update (list_progress, or create_progress_update "
                + "for a day with none yet); sitePhotoIds are the pool ids match_site_photos "
                + "returned — never file names or hashes. One update per call: group the week's "
                + "matches by the day the WhatsApp export puts each photo on and call once per "
                + "day. Up to fifty per call. A photo the export puts on two days goes on the "
                + "first — the second filing is refused per photo, which is fine."),

        new AiAction(
            Name: "delete_site_photo",
            Area: SitePhotoArea,
            Description: "Deletes one photograph from the site photo pool permanently, with its "
                + "stored file. A copy already filed onto a progress update is that update's own "
                + "photo and stays. There is no undo.",
            CommandType: typeof(DeleteSitePhoto),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteSitePhotoAuthorisation),
            ValidationType: null,
            VisibleTo: ProgressRoles.Contributors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which photo, by file name, before calling. Filed photos "
                + "need no clearing out — list_site_photos with unfiledOnly is the working view.")
    };
}
