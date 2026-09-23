using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Hs.Thread;

internal static class HsRecordThreadMapping
{
    public static HsRecordComment ToModel(this HsRecordCommentEntity entity, IEnumerable<HsRecordPhotoEntity> photos) => new(
        entity.HsRecordCommentId, entity.HsRecordId, entity.AuthorEmail, entity.AuthorName, entity.Text, entity.PostedAt,
        photos.OrderBy(photo => photo.UploadedAt).Select(photo => photo.ToModel()).ToList());

    public static HsRecordPhoto ToModel(this HsRecordPhotoEntity entity) => new(
        entity.HsRecordPhotoId, entity.HsRecordId, entity.HsRecordCommentId, entity.FileName, entity.ContentType, entity.UploadedAt);
}
