namespace Jewel.JPMS.Api.Features.Drawings;

/// <summary>
/// Column widths the drawing validations check against, so an over-long optional field is a 400
/// with a message instead of a SQL truncation error. Mirrors the MaxLength attributes on the
/// entities in CoreEntities.cs.
/// </summary>
internal static class DrawingFieldLimits
{
    public const int DrawingCodeMaxLength = 64;
    public const int TitleMaxLength = 256;
    public const int RevisionLabelMaxLength = 16;
    public const int EmailMaxLength = 256;
    public const int FolderNameMaxLength = 128;
}
