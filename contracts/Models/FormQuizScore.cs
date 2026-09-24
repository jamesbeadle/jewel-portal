namespace Jewel.JPMS.Models;

/// <summary>
/// A quiz as the api marked it: the points scored, the points there were and the mark that passes.
/// Derived from the stored answers every time it is read, never stored, so a corrected answer key
/// re-marks every quiz already sent.
/// </summary>
public sealed record FormQuizScore(int Score, int OutOf, int PassMark)
{
    public bool HasPassed => Score >= PassMark;

    public string Outcome => HasPassed ? "passed" : "failed";

    public string Sentence => $"{Score}/{OutOf}, {Outcome} (pass mark {PassMark})";
}
