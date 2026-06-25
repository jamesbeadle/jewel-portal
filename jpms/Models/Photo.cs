namespace Jewel.JPMS.Models;

public enum PhotoAttachedKind
{
    Project,
    Defect,
    HsRecord,
    Request,
    WalkRoundNote,
    SiteReport
}

public sealed record Photo(
    string PhotoId,
    string ProjectId,
    PhotoAttachedKind AttachedKind,
    string? AttachedId,
    string BlobUri,
    string Caption,
    string TakenByEmail,
    DateTimeOffset TakenAt,
    decimal? GpsLatitude,
    decimal? GpsLongitude);
