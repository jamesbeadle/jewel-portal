using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Leads;

internal static class LeadEntityMapping
{
    public static Lead ToModel(this LeadEntity entity) => new(
        LeadId: entity.LeadId,
        Reference: entity.Reference,
        ContactName: entity.ContactName,
        ContactEmail: entity.ContactEmail,
        ContactPhone: entity.ContactPhone,
        CompanyName: entity.CompanyName,
        SiteAddress: entity.SiteAddress,
        EstimatedValue: entity.EstimatedValue,
        Source: (LeadSource)entity.Source,
        Stage: (LeadStage)entity.Stage,
        OwnerEmail: entity.OwnerEmail,
        CapturedAt: entity.CapturedAt);

    public static QualificationAssessment ToModel(this QualificationAssessmentEntity entity) =>
        new(entity.LeadId, entity.Score, entity.Notes, entity.AssessedByEmail, entity.AssessedAt);

    public static SiteVisit ToModel(this SiteVisitEntity entity) => new(
        SiteVisitId: entity.SiteVisitId,
        LeadId: entity.LeadId,
        ScheduledAt: entity.ScheduledAt,
        AttendeeEmails: entity.AttendeeEmailsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        Notes: entity.Notes,
        PhotoCount: entity.PhotoCount,
        IsComplete: entity.IsComplete);

    public static InfoChaseItem ToModel(this InfoChaseItemEntity entity) =>
        new(entity.InfoChaseItemId, entity.LeadId, entity.Kind, entity.Description, entity.IsReceived, entity.RequestedAt);

    public static BidDecision ToModel(this BidDecisionEntity entity) =>
        new(entity.LeadId, entity.ShouldBid, entity.Reason, entity.DecidedByEmail, entity.DecidedAt);

    public static Proposal ToModel(this ProposalEntity entity) =>
        new(entity.ProposalId, entity.LeadId, entity.Value, entity.IssuedAt, Array.Empty<NegotiationRound>());

    public static LeadOutcome ToModel(this LeadOutcomeEntity entity) =>
        new(entity.LeadId, entity.IsWon, entity.Reason, entity.DecidedByEmail, entity.DecidedAt, entity.CreatedProjectId);
}
