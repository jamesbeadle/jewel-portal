using System.Linq;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The pathway (bucket) rules the 2026-08-27 Control Centre restructure leans on: the four
// buckets, the Supplier pathway's wiring, and the Materials category's move from the
// Subcontractor family to the Supplier family with its SubComms-Mats tag stem intact (so mail
// tagged before the split keeps reading back).
public sealed class TriagePathwayTests
{
    [Fact]
    public void FourBuckets_inDisplayOrder()
    {
        Assert.Equal(
            new[]
            {
                TriageCategories.Client, TriageCategories.Subcontractor,
                TriageCategories.Supplier, TriageCategories.Internal
            },
            TriageCategories.AllBuckets);
    }

    [Theory]
    [InlineData("JPMS/Client")]
    [InlineData("JPMS/Subcontractor")]
    [InlineData("JPMS/Supplier")]
    [InlineData("JPMS/Internal")]
    public void EveryBucket_isABucketTag(string bucket) =>
        Assert.True(TriageCategories.IsBucketTag(bucket));

    [Fact]
    public void SupplierComms_filesUnderTheSupplierPathway() =>
        Assert.Equal(TriageCategories.Supplier, TriageCategories.BucketFor(RecordType.SupplierComms));

    [Fact]
    public void SupplierBucket_readsBackAsSupplier() =>
        Assert.Equal("Supplier", AuditTrail.PathwayLabel(TriageCategories.Supplier));

    [Fact]
    public void SupplierRoute_resolvesToTheSupplierFamily()
    {
        Assert.Same(CommunicationFamily.Supplier, CommunicationFamily.ForRoute("/suppliers/communications"));
        // Category deep links resolve to their family too.
        Assert.Same(CommunicationFamily.Supplier, CommunicationFamily.ForRoute("/suppliers/communications/materials"));
        Assert.Same(CommunicationFamily.Internal, CommunicationFamily.ForRoute("/internal/communications/site-instruction"));
    }

    [Fact]
    public void Materials_movedToTheSupplierFamily_keepingItsTagStem()
    {
        var materials = Assert.Single(SupplierComms.Categories, record => record.Title == "Materials");
        Assert.Equal(RecordType.SupplierComms, materials.Type);
        Assert.Equal("JPMS/SubComms-Mats", CommunicationFamily.TagFor(materials));
        // …and left the subcontractor family entirely.
        Assert.DoesNotContain(SubcontractorComms.All, record => record.Title == "Materials");
        Assert.Contains("JPMS/SubComms-Mats", CommunicationFamily.Supplier.Tags);
    }

    [Fact]
    public void SupplierFamily_offersMaterialsAndFinishes_notGeneral()
    {
        // General survives as the family's structural base (tag stem, route) but the UI offers
        // only the categories — every supplier email is a Materials or Finishes matter.
        Assert.Equal(new[] { "Materials", "Finishes" },
            CommunicationFamily.Supplier.Offered.Select(record => record.Title).ToArray());
        Assert.Contains("JPMS/SupComms-Fin", CommunicationFamily.Supplier.Tags);
        // The other families still lead with General.
        Assert.Equal(CommunicationFamily.Subcontractor.All, CommunicationFamily.Subcontractor.Offered);
        Assert.Equal(CommunicationFamily.Internal.All, CommunicationFamily.Internal.Offered);
    }

    [Fact]
    public void InternalCategories_areEmpty_siteInstructionIsARecordNow()
    {
        // Site instruction left the record-less family 2026-09-03: it is a real project record
        // (RecordType.SiteInstruction, SI-####) with the instruction written into it.
        Assert.Empty(InternalComms.Categories);
        Assert.Equal(new[] { InternalComms.Record }, InternalComms.All);
    }

    [Fact]
    public void CategorySlugs_roundTripThroughTheirFamily()
    {
        foreach (var family in CommunicationFamily.Known)
        foreach (var category in family.Categories)
        {
            var slug = CommunicationFamily.Slug(category);
            Assert.Same(category, family.ForSlug(slug));
            Assert.EndsWith($"{family.Route}/{slug}", family.RouteFor(category));
        }
        // The general record's register is the family route itself.
        Assert.Equal("/suppliers/communications", CommunicationFamily.Supplier.RouteFor(SupplierComms.Record));
        // A blank or unknown segment means the whole family.
        Assert.Null(CommunicationFamily.Supplier.ForSlug(null));
        Assert.Null(CommunicationFamily.Supplier.ForSlug("not-a-category"));
    }
}
