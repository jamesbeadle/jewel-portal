using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One itemised query on a request's official document (the numbered rows of an RFI sheet:
/// Item / Drawing Ref / Member-Area / Query / Response). Items belong to exactly one request and
/// are ordered by <see cref="Position"/>; the rendered item number is the 1-based position, so
/// deleting a row renumbers the ones after it — matching how JBB's Excel RFI sheets behave.
/// Lives in the api project's entity folder, which the worker links into its own compilation, so
/// both Function apps share one definition.
/// </summary>
public sealed class RequestItemEntity
{
    [Key, MaxLength(64)] public string RequestItemId { get; set; } = "";
    [MaxLength(64)]      public string RequestId { get; set; } = "";

    // 1-based display order; the rendered "Item" number.
    public int Position { get; set; }

    // Drawing / detail / email references the item concerns (free text, may span several lines).
    [MaxLength(1024)]    public string DrawingRef { get; set; } = "";

    // The member or area of the works the item concerns (e.g. "West Elevation — lead valley gutter").
    [MaxLength(512)]     public string MemberArea { get; set; } = "";

    // The query itself — what instruction / confirmation is required.
    [MaxLength(4000)]    public string Query { get; set; } = "";

    // The architect's / respondent's answer, captured when it comes back. Null until answered.
    [MaxLength(4000)]    public string? Response { get; set; }
}
