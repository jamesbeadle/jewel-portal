using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.Sales.Imagine;
using Jewel.JPMS.Api.Features.Sales.Research;
using Jewel.JPMS.Api.Storage;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jewel.JPMS.Tests;

// The Imagine render's two exits that are not its own doing (2026-09-16): the host's
// functionTimeout cancels the run — the round must read Failed with a reason and a line on the
// lead's timeline, never sit on Rendering — and the host's retry of that dead run is ignored.
public sealed class ImagineRenderRunnerTests
{
    private const string LeadId = "lead-1";
    private const string RoundId = "round-1";

    [Fact]
    public async Task AHostCancellation_stampsTheRoundFailed_withTheReason_andTellsTheTimeline_thenRethrows()
    {
        await using var context = await ContextWithAQueuedRoundAsync();
        var runner = RunnerOver(context, new StoreThatIsCancelledMidRead());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            runner.RunAsync(new ImagineRenderMessage(RoundId), CancellationToken.None));

        var round = await context.ImagineRounds.SingleAsync(row => row.RoundId == RoundId);
        Assert.Equal((int)ImagineRoundStatus.Failed, round.Status);
        Assert.NotNull(round.CompletedAt);
        Assert.Contains("timed out", round.Error);
        var timeline = await context.LeadActivities.Where(row => row.LeadId == LeadId).ToListAsync();
        Assert.Contains(timeline, activity => activity.Summary.StartsWith("Imagine round 1 failed: The render timed out"));
    }

    [Fact]
    public async Task TheHostsRetryOfAFailedRound_isIgnored()
    {
        await using var context = await ContextWithAQueuedRoundAsync();
        var round = await context.ImagineRounds.SingleAsync(row => row.RoundId == RoundId);
        round.Status = (int)ImagineRoundStatus.Failed;
        round.Error = "The render timed out after 10 minutes";
        await context.SaveChangesAsync();
        var store = new StoreThatIsCancelledMidRead();

        await RunnerOver(context, store).RunAsync(new ImagineRenderMessage(RoundId), CancellationToken.None);

        Assert.Equal(0, store.Reads);
        Assert.Equal((int)ImagineRoundStatus.Failed, round.Status);
        Assert.Equal("The render timed out after 10 minutes", round.Error);
    }

    private static ImagineRenderRunner RunnerOver(JpmsContext context, IImagineImageStore store) =>
        new(context, store,
            new ImagineConceptWriter(new HttpClient(), new AnthropicOptions(), NullLogger<ImagineConceptWriter>.Instance),
            new ImagesNeverReached(), new NotifierNeverReached(), new ImagineNotifierOptions(),
            NullLogger<ImagineRenderRunner>.Instance);

    private static async Task<JpmsContext> ContextWithAQueuedRoundAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"imagine-render-{Guid.NewGuid():N}").Options);
        context.Leads.Add(new LeadEntity { LeadId = LeadId, Number = 1, ContactName = "Jane Coombe", Stage = (int)LeadStage.Engaged });
        context.ImagineRounds.Add(new ImagineRoundEntity
        {
            RoundId = RoundId, LeadId = LeadId, Number = 1, Kind = (int)ImagineRoundKind.Concepts,
            Status = (int)ImagineRoundStatus.Queued, RequestedAt = DateTimeOffset.UtcNow
        });
        context.ImagineImages.Add(new ImagineImageEntity
        {
            ImageId = "photo-1", LeadId = LeadId, RoundId = RoundId, Kind = (int)ImagineImageKind.Photo,
            Order = 1, BlobRef = "photos/photo-1.jpg", ContentType = "image/jpeg", CreatedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();
        return context;
    }

    private sealed class StoreThatIsCancelledMidRead : IImagineImageStore
    {
        public int Reads { get; private set; }
        public bool IsConfigured => true;
        public Task<string> SaveAsync(string leadId, string roundId, string imageId, string contentType, byte[] bytes, CancellationToken ct) => throw new NotSupportedException();
        public Task<StoredBlob?> OpenAsync(string blobRef, CancellationToken ct) => throw new NotSupportedException();
        public Task<byte[]?> ReadAllAsync(string blobRef, CancellationToken ct)
        {
            Reads++;
            throw new OperationCanceledException("The host stopped the run.");
        }
    }

    private sealed class ImagesNeverReached : IAzureImageClient
    {
        public bool IsConfigured => true;
        public Task<RenderedImage> EditAsync(IReadOnlyList<ImageInput> references, string prompt, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class NotifierNeverReached : IImagineNotifier
    {
        public bool IsConfigured => false;
        public Task SendConceptsReadyAsync(string toEmail, string toName, string link, int conceptCount, bool revision, CancellationToken ct) => Task.CompletedTask;
        public Task SendProposalAsync(string toEmail, string toName, string link, string title, string? note, CancellationToken ct) => Task.CompletedTask;
        public Task SendToSalesAsync(string subject, string html, string text, CancellationToken ct) => Task.CompletedTask;
    }
}
