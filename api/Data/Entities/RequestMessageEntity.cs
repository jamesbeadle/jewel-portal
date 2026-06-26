using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class RequestMessageEntity
{
    [Key, MaxLength(64)] public string MessageId { get; set; } = "";
    [MaxLength(64)]      public string RequestId { get; set; } = "";
    [MaxLength(256)]     public string AuthorEmail { get; set; } = "";
    [MaxLength(256)]     public string AuthorName { get; set; } = "";
    [MaxLength(4000)]    public string Body { get; set; } = "";
    public int Visibility { get; set; }
    public DateTimeOffset PostedAt { get; set; }
}
