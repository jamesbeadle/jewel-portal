using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.UsefulInformation;

internal static class UsefulInformationEntityMapping
{
    public static UsefulInformationNote ToModel(this UsefulInformationNoteEntity entity) =>
        new(entity.UsefulInformationNoteId,
            entity.ProjectId,
            entity.Title,
            entity.Body,
            entity.CreatedByEmail,
            entity.CreatedAt,
            entity.UpdatedByEmail,
            entity.UpdatedAt);
}
