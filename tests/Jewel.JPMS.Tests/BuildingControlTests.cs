using System;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.BuildingControl;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Building control's rules: the BC-####/BCI-#### reference/tag round-trips, the shared
// case/inspection validation, the inspection status ladder's date stamping, the copied-file kind
// inference, the one-active-case rule, and the client-side pathway both record types file under.
public sealed class BuildingControlTests
{
    private static BuildingControlCaseDetails CaseDetails(
        string bodyName = "Assent Building Control",
        DateTimeOffset? submitted = null,
        DateTimeOffset? accepted = null) =>
        new(BuildingControlRegime.RegisteredApprover, bodyName, "25-129527",
            "Jane Inspector", "jane@assentbc.co.uk", "", submitted, accepted, "");

    private static BuildingControlInspectionDetails InspectionDetails(
        string stageName = "Foundations",
        DateTimeOffset? bookedFor = null) =>
        new(stageName, bookedFor, null, "", "");

    private static DateTimeOffset Day(int year, int month, int day) =>
        new(new DateTime(year, month, day), TimeSpan.Zero);

    // ---- References / tags ----

    [Fact]
    public void References_formatOnTheGlobalSequences()
    {
        Assert.Equal("BC-0002", new BuildingControlCaseEntity { Number = 2 }.Reference);
        Assert.Equal("BCI-0014", new BuildingControlInspectionEntity { Number = 14 }.Reference);
    }

    [Fact]
    public void References_fallBackToIdStemsWhenUnnumbered()
    {
        // Two unnumbered rows must never share a stem (there should be none in practice).
        var a = new BuildingControlCaseEntity { BuildingControlCaseId = "aaaa1111bbbb" };
        var b = new BuildingControlCaseEntity { BuildingControlCaseId = "cccc2222dddd" };
        Assert.NotEqual(a.Reference, b.Reference);
        Assert.StartsWith("BC-", a.Reference);
    }

    [Theory]
    [InlineData("BC-0001", "BC", 1)]
    [InlineData("BCI-0031", "BCI", 31)]
    public void TagReferences_roundTripThroughTheParser(string tag, string prefix, int expected)
    {
        Assert.True(TagReferenceParsing.TryParseNumber(tag, prefix, out var number));
        Assert.Equal(expected, number);
    }

    [Fact]
    public void TagReferences_neverCrossFamilies()
    {
        // "BC" must not swallow "BCI-…" stems (and vice versa) — the two families share a prefix
        // character run, so the parser's exact-prefix rule is what keeps them apart.
        Assert.False(TagReferenceParsing.TryParseNumber("BCI-0031", "BC", out _));
        Assert.False(TagReferenceParsing.TryParseNumber("BC-0001", "BCI", out _));
        Assert.False(TagReferenceParsing.TryParseNumber("DEF-0001", "BC", out _));
    }

    // ---- Pathway ----

    [Fact]
    public void BothRecordTypes_fileUnderTheClientPathway()
    {
        Assert.Equal(TriageCategories.Client, TriageCategories.BucketFor(RecordType.BuildingControlCase));
        Assert.Equal(TriageCategories.Client, TriageCategories.BucketFor(RecordType.BuildingControlInspection));
    }

    // ---- Case rules ----

    [Fact]
    public void CaseValidation_requiresABodyName() =>
        Assert.Contains(BuildingControlRules.CaseProblems(CaseDetails(bodyName: " ")),
            error => error.Contains("body", StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void CaseValidation_refusesAcceptanceBeforeSubmission() =>
        Assert.NotEmpty(BuildingControlRules.CaseProblems(
            CaseDetails(submitted: Day(2026, 5, 10), accepted: Day(2026, 5, 1))));

    [Fact]
    public void CaseValidation_passesACompleteCase() =>
        Assert.Empty(BuildingControlRules.CaseProblems(
            CaseDetails(submitted: Day(2026, 4, 1), accepted: Day(2026, 4, 15))));

    [Theory]
    [InlineData(BuildingControlCaseStatus.NoticeSubmitted, true)]
    [InlineData(BuildingControlCaseStatus.InForce, true)]
    [InlineData(BuildingControlCaseStatus.CompletionRequested, true)]
    [InlineData(BuildingControlCaseStatus.CompletionCertified, false)]
    [InlineData(BuildingControlCaseStatus.Lapsed, false)]
    public void OnlyAFinishedCase_maysBeSucceeded(BuildingControlCaseStatus status, bool active) =>
        Assert.Equal(active, BuildingControlRules.IsActive(new BuildingControlCaseEntity { Status = (int)status }));

    [Fact]
    public void CaseDates_normaliseToMidnightUtc()
    {
        var entity = new BuildingControlCaseEntity();
        BuildingControlRules.Apply(entity, CaseDetails(
            submitted: new DateTimeOffset(2026, 5, 10, 14, 30, 0, TimeSpan.FromHours(1))));
        Assert.Equal(Day(2026, 5, 10), entity.NoticeSubmittedOn);
    }

    // ---- Inspection rules ----

    [Fact]
    public void InspectionValidation_requiresAStageName() =>
        Assert.NotEmpty(BuildingControlRules.InspectionProblems(InspectionDetails(stageName: " ")));

    [Fact]
    public void ANewStage_startsPlanned_orBookedWhenDated()
    {
        Assert.Equal(BuildingControlInspectionStatus.Planned,
            BuildingControlRules.StatusOnAdd(InspectionDetails()));
        Assert.Equal(BuildingControlInspectionStatus.Booked,
            BuildingControlRules.StatusOnAdd(InspectionDetails(bookedFor: Day(2026, 9, 3))));
    }

    [Fact]
    public void MovingToInspected_stampsTheVisitDateOnce()
    {
        var entity = new BuildingControlInspectionEntity { Status = (int)BuildingControlInspectionStatus.Booked };
        BuildingControlRules.ApplyStatus(entity, BuildingControlInspectionStatus.Inspected);
        Assert.NotNull(entity.InspectedAt);

        // A recorded visit date is the official fact — a later move must not re-stamp it.
        var recorded = Day(2026, 9, 3);
        entity.InspectedAt = recorded;
        BuildingControlRules.ApplyStatus(entity, BuildingControlInspectionStatus.Passed);
        Assert.Equal(recorded, entity.InspectedAt);
    }

    [Fact]
    public void MovingBackToPlannedOrBooked_clearsTheVisitDate()
    {
        var entity = new BuildingControlInspectionEntity
        {
            Status = (int)BuildingControlInspectionStatus.Inspected,
            InspectedAt = Day(2026, 9, 3)
        };
        BuildingControlRules.ApplyStatus(entity, BuildingControlInspectionStatus.Booked);
        Assert.Null(entity.InspectedAt);
    }

    // ---- Copied-file kind inference ----

    [Theory]
    [InlineData("image/jpeg", "site.jpg", BuildingControlAttachmentKind.Photo)]
    [InlineData("application/pdf", "report.pdf", BuildingControlAttachmentKind.SiteInspectionReport)]
    [InlineData("application/octet-stream", "Site Inspection Report.PDF", BuildingControlAttachmentKind.SiteInspectionReport)]
    [InlineData("application/msword", "notes.doc", BuildingControlAttachmentKind.Other)]
    public void CopiedFiles_inferTheirKindFromTheirType(
        string contentType, string fileName, BuildingControlAttachmentKind expected) =>
        Assert.Equal(expected, BuildingControlRules.InferKind(contentType, fileName));

    // ---- Seed checklist ----

    [Fact]
    public void TheDefaultChecklist_isANonEmptyTemplate()
    {
        Assert.NotEmpty(BuildingControlStages.DefaultChecklist);
        // The seed ends at Completion — the certificate is the case's whole point.
        Assert.Equal("Completion", BuildingControlStages.DefaultChecklist[^1]);
    }
}
