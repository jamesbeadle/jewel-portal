using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Pages;

public partial class ProjectProgramme
{
    private async Task LoadLadsAsync()
    {
        try
        {
            lads = await Queries.AskAsync(new ListLadClaimsForProject(ProjectId), CancellationToken.None);
            ladsFailed = false;
        }
        catch
        {
            lads = Array.Empty<LadClaim>();
            ladsFailed = true;
            claimsError = "Couldn't load the LADs claims. Please try again.";
        }
        finally
        {
            ladsLoaded = true;
        }
    }

    private async Task LoadEmailsAsync()
    {
        emailsError = null;
        try
        {
            emails = await Queries.AskAsync(new ListSchedulingEmails(ProjectId), CancellationToken.None);
        }
        catch
        {
            emails = Array.Empty<MailboxMessage>();
            emailsError = "Couldn't load programme emails. Please try again.";
        }
        finally
        {
            emailsLoaded = true;
        }
    }

}
