namespace Jewel.JPMS.Models;

public sealed record BoqSignOff(
    string BoqSignOffId,
    string ProjectId,
    string SignedOffByEmail,
    DateTimeOffset SignedOffAt,
    decimal TenderTotalAtSignOff);
