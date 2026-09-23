namespace Jewel.JPMS.Models;

/// <summary>
/// The office's check of a Company Vehicle Form: the DVLA record viewed with the person's check
/// code, and whether they meet the insurance criteria. The form's own privacy paragraph promises
/// that only that outcome is kept — so recording the check deletes the licence photograph and
/// withholds the driving-record answers and the check code from the stored form for good.
/// </summary>
public sealed record DrivingLicenceCheck(
    string DrivingLicenceCheckId,
    string FormSubmissionId,
    DateOnly DvlaCheckedOn,
    bool IsWithinInsuranceCriteria,
    string Note,
    string CheckedByEmail,
    DateTimeOffset CheckedAt,
    int PhotosDeleted);

public static class DrivingLicenceChecks
{
    public const int DaysACheckCodeLasts = 21;
    public const string Withheld = "Withheld after the licence check";
    public const string LicencePhotoKey = "licence";

    public static readonly string[] AnswersWithheldAfterTheCheck =
    {
        "dvla_code", "points", "points_detail", "disqualified", "disqualified_detail", "tacho", "unspent", "medical", "eyesight"
    };

    public static int CheckCodeDaysLeft(DateTimeOffset submittedAt, DateTimeOffset now) =>
        DaysACheckCodeLasts - (int)(now - submittedAt).TotalDays;
}
