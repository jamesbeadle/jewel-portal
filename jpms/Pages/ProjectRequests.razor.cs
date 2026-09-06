using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectRequests
{
    [Parameter] public string ProjectId { get; set; } = "";
    [Parameter] public string? Kind { get; set; }


    // ---- Manual RFI entry ----------------------------------------------------------------------
    // The RFI-locked raise dialog (attachments and all). Most RFIs are raised from an email in
    // the Control Centre; this is the way in for one with no email behind it, or a legacy
    // back-fill via the form's "Log a historical RFI" tick.

    private bool raiseDialogOpen;

    private void OpenRaiseDialog() => raiseDialogOpen = true;
    private void CloseRaiseDialog() => raiseDialogOpen = false;

    private void OnRaised(Request raised)
    {
        raiseDialogOpen = false;
        Nav.NavigateTo($"/projects/{ProjectId}/requests/view/{raised.RequestId}");
    }

    private IReadOnlyList<Request> AllRecords => RequestRegister.ForProject(ProjectId);
    private int OpenCount => AllRecords.Count(r => r.Status is not RequestStatus.Closed);
    private int OverdueCount => AllRecords.Count(r => r.IsOverdue);
    private int GeneralCount => AllRecords.Count(r => r.Kind == RequestType.General);
    private int RfiCount => AllRecords.Count(r => r.Kind == RequestType.Rfi);
    private int OverdueRfiCount => AllRecords.Count(r => r.Kind == RequestType.Rfi && r.IsOverdue);

}
