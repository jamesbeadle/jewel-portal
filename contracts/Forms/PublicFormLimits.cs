namespace Jewel.JPMS.Contracts.Forms;

/// <summary>The dashboard's limits for a public form: the same caps a stranger's upload was held to there.</summary>
public static class PublicFormLimits
{
    public const int LargestUploadBytes = 2_800_000;
    public const int LongestEncodedUpload = 3_600_000;
    public const int LongestAnswer = 4000;
    public const int LongestTextBox = 2000;
    public const int MostFilesPerSession = 40;
    public const int ShortestSessionId = 16;
    public const int LongestSessionId = 32;
    public const int SessionIdBytes = 12;
    public const long LargestFileChosen = 15_000_000;
    public const long ShrinkPhotosOver = 1_500_000;
    public const int LongestPhotoEdge = 1600;
    public const int LongestEncodedOnThePage = 3_400_000;
}
