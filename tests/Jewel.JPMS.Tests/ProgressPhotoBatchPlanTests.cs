using ImageMagick;
using Jewel.JPMS.Api.Features.Progress.Photos;
using Jewel.JPMS.Contracts.Progress;
using Xunit;

namespace Jewel.JPMS.Tests;

// A photo batch (2026-09-16, the FD's weekly-report spec, change 2): deduplicated on the content
// of the file as received — against what the update holds and within the batch — never on the
// name; a file that is not an image fails on its own and the batch carries on; a stored image is
// shrunk to display size and comes out as JPEG or PNG.
public sealed class ProgressPhotoBatchPlanTests
{
    private static readonly IReadOnlyDictionary<string, string> NothingHeld = new Dictionary<string, string>();

    private static byte[] Png(int width, int height, MagickColor colour)
    {
        using var image = new MagickImage(colour, width, height);
        image.Format = MagickFormat.Png;
        return image.ToByteArray();
    }

    [Fact]
    public void SameContentTwice_isStoredOnce_whateverTheFileName()
    {
        var picture = Png(4, 4, MagickColors.Red);
        var images = new[]
        {
            new IncomingProgressPhoto("IMG-0001.png", "image/png", picture),
            new IncomingProgressPhoto("IMG-0002.png", "image/png", picture),
            new IncomingProgressPhoto("IMG-0003.png", "image/png", Png(4, 4, MagickColors.Blue)),
        };

        var plan = ProgressPhotoBatchPlan.Build(images, NothingHeld);

        Assert.Equal(new[] { ProgressPhotoIntakeResult.Stored, ProgressPhotoIntakeResult.Duplicate, ProgressPhotoIntakeResult.Stored },
            plan.Select(step => step.Outcome.Result));
        Assert.Contains("IMG-0001.png", plan[1].Outcome.Detail);
    }

    [Fact]
    public void ContentTheUpdateAlreadyHolds_isADuplicate()
    {
        var picture = Png(4, 4, MagickColors.Red);
        var held = new Dictionary<string, string> { [ProgressPhotoContentHash.Of(picture)] = "earlier.jpg" };

        var plan = ProgressPhotoBatchPlan.Build(new[] { new IncomingProgressPhoto("again.png", "image/png", picture) }, held);

        Assert.Equal(ProgressPhotoIntakeResult.Duplicate, plan.Single().Outcome.Result);
        Assert.Contains("earlier.jpg", plan.Single().Outcome.Detail);
    }

    [Fact]
    public void AFileThatIsNotAnImage_failsAlone_andTheBatchCarriesOn()
    {
        var images = new[]
        {
            new IncomingProgressPhoto("notes.pdf", "application/pdf", new byte[] { 1, 2, 3 }),
            new IncomingProgressPhoto("broken.jpg", "image/jpeg", new byte[] { 1, 2, 3 }),
            new IncomingProgressPhoto("ok.png", "image/png", Png(4, 4, MagickColors.Green)),
        };

        var plan = ProgressPhotoBatchPlan.Build(images, NothingHeld);

        Assert.Equal(ProgressPhotoIntakeResult.Failed, plan[0].Outcome.Result);
        Assert.Equal(ProgressPhotoIntakeResult.Failed, plan[1].Outcome.Result);
        Assert.Equal(ProgressPhotoIntakeResult.Stored, plan[2].Outcome.Result);
        Assert.NotNull(plan[2].Prepared);
    }

    [Fact]
    public void ALargeImage_isShrunkToDisplaySize_andHashedOnWhatArrived()
    {
        var original = Png(ProgressPhotoLimits.MaxEdgePixels * 2, ProgressPhotoLimits.MaxEdgePixels, MagickColors.Red);

        var prepared = ProgressPhotoPreparation.Prepare(new IncomingProgressPhoto("wide.png", "image/png", original));

        using var stored = new MagickImage(prepared.Bytes);
        Assert.Equal(ProgressPhotoLimits.MaxEdgePixels, (int)stored.Width);
        Assert.Equal("image/png", prepared.ContentType);
        Assert.Equal(ProgressPhotoContentHash.Of(original), prepared.ContentHash);
        Assert.Equal("wide.png", prepared.FileName);
    }
}
