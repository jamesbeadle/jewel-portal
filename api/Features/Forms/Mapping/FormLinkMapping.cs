using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Forms.Mapping;

internal static class FormLinkMapping
{
    public static FormInvite ToModel(this FormInviteEntity entity) => new(
        entity.FormInviteId, entity.FormPackId, entity.FormSlug, entity.PersonName,
        entity.CompanyName, entity.Email, entity.SentByName, entity.SentAt, entity.ExpiresAt, entity.OpenedAt,
        entity.UsedAt, entity.FormSubmissionId, entity.CancelledAt, entity.PolicyDocumentId);

    public static FormPack ToModel(this FormPackEntity entity, IEnumerable<FormInviteEntity> invites) => new(
        entity.FormPackId, entity.PersonName, entity.Email, (Engagement)entity.EngagedAs,
        entity.SentByName, entity.SentAt, entity.ExpiresAt, entity.OpenedAt, entity.CompletedAt, entity.LastChasedAt,
        entity.ChaseCount, entity.CancelledAt,
        invites.Where(invite => invite.FormPackId == entity.FormPackId).Select(invite => invite.ToModel()).ToList());

    public static FormPackAnswers AnswersOf(this FormPackEntity entity) =>
        new(entity.HasP45, entity.IsWorkingAtAScreen, entity.IsGettingAVehicle, entity.MustHoldATicket);
}
