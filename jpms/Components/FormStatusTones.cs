namespace Jewel.JPMS.Components;

/// <summary>
/// The forms' statuses in the tone vocabulary — StatusTones' forms chapter, kept beside it so a new
/// starter's pack, a certificate and a workstation action read in the same colours everywhere.
/// </summary>
public static class FormStatusTones
{
    public static Tone ToTone(this FormSubmissionStatus status) => status switch
    {
        FormSubmissionStatus.New => Tone.Info,
        FormSubmissionStatus.InProgress => Tone.Warning,
        FormSubmissionStatus.Handled => Tone.Positive,
        _ => Tone.Muted
    };

    public static Tone ToTone(this FormLinkState state) => state switch
    {
        FormLinkState.Opened => Tone.Info,
        FormLinkState.Done => Tone.Positive,
        FormLinkState.Expired => Tone.Warning,
        _ => Tone.Muted
    };

    public static Tone ToTone(this TrainingStanding standing) => standing switch
    {
        TrainingStanding.Valid => Tone.Positive,
        TrainingStanding.ExpiringSoon => Tone.Warning,
        TrainingStanding.Expired => Tone.Negative,
        _ => Tone.Muted
    };

    public static Tone ToTone(this WorkstationActionState state) => state switch
    {
        WorkstationActionState.Open => Tone.Warning,
        WorkstationActionState.Fixed => Tone.Positive,
        _ => Tone.Info
    };

    public static Tone ToTone(this RightToWorkOutcome outcome) => outcome switch
    {
        RightToWorkOutcome.Pass => Tone.Positive,
        RightToWorkOutcome.QueryDoNotStart => Tone.Warning,
        _ => Tone.Negative
    };
}
