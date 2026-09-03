using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.SiteInstructions;

internal static class SiteInstructionEntityMapping
{
    public static SiteInstruction ToModel(this SiteInstructionEntity entity) =>
        new(entity.SiteInstructionId, entity.ProjectId, entity.Title, entity.Instruction,
            entity.Location, entity.CreatedAt, entity.Reference);
}
