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
            Name: "archive_site_photos",
            Area: SitePhotoArea,
            Description: "Sets photographs in the site photo pool aside from the weekly "
                + "Contractor's Report and KEEPS them — the week's dump that is not progress: "
                + "screenshots, drawings and documents, photos someone has drawn over, snags and "
                + "defects, and photos with no reason to be in a report. Each photo carries its "
                + "reason and a note saying why; the batch carries the project and the "
                + "Friday-to-Thursday week it was dumped for. An archived photo leaves the "
                + "waiting view, is never filed, and can be restored. One outcome per photo: "
                + "archived, already archived, already filed (refused — it is on an update), or "
                + "not found.",
            CommandType: typeof(ArchiveSitePhotos),
            ResultType: typeof(SitePhotoArchivingResult),
            AuthorisationType: typeof(ArchiveSitePhotosAuthorisation),
            ValidationType: typeof(ArchiveSitePhotosValidation),
            VisibleTo: ProgressRoles.Contributors,
            EmailStamps: new[] { "ArchivedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "Judge each photo by the jpms-contractors-report skill's criteria for what never "
                + "goes in the report — load it first. sitePhotoIds are the pool ids "
                + "match_site_photos returned; the note is one sentence the site manager would "
                + "recognise (\"screenshot of the kitchen drawing\", \"WhatsApp calls it a snag on "
                + "the landing\"). projectId and periodEnd (the Thursday that ends the week) are "
                + "the report's. List what you archived, by file name and reason, before filing "
                + "the rest. Up to fifty per call."),

        new AiAction(
            Name: "restore_site_photo",
            Area: SitePhotoArea,
            Description: "Brings one archived photograph back into the site photo pool's waiting "
                + "view so it can be filed onto a progress update — for a photo the weekly-report "
                + "run set aside that belongs in the report after all.",
            CommandType: typeof(RestoreSitePhoto),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RestoreSitePhotoAuthorisation),
            ValidationType: null,
            VisibleTo: ProgressRoles.Contributors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "list_site_photos with archivedOnly lists the archive with each photo's reason."),

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
