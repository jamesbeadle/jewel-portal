using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Bluebeam.Extraction;
using Jewel.JPMS.Api.Features.Bluebeam.Queue;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jewel.JPMS.Tests;

// 2026-09-16: every revision that lands is queued for transcription — best effort, after the
// landing's own save, so a queue that is down never fails a filing or an upload.
public sealed class DrawingExtractionAutoQueueTests
{
    [Fact]
    public async Task ALandedPdf_getsAQueuedRow_andOneMessage()
    {
        await using var context = NewContext();
        var queue = new RecordingQueue();
        var revision = DrawingDataRowsTests.Revision("rev-1", "dwg-1", "A", DateTimeOffset.UtcNow);

        await AutoQueue(context, queue).QueueLandedRevisionAsync(revision, "proj-1", "pm@jewel.test", CancellationToken.None);

        var row = await context.DrawingExtractions.SingleAsync();
        Assert.Equal((int)DrawingExtractionStatus.Queued, row.Status);
        Assert.Equal("pm@jewel.test", row.QueuedBy);
        Assert.Equal("proj-1", row.ProjectId);
        var message = Assert.Single(queue.Sent);
        Assert.Equal("rev-1", message.DrawingRevisionId);
        Assert.False(message.Force);
        Assert.False(message.RowsOnly);
    }

    [Fact]
    public async Task AQueueThatIsDown_leavesAnHonestFailedRow_andDoesNotThrow()
    {
        await using var context = NewContext();
        var queue = new RecordingQueue { Failure = new InvalidOperationException("storage queue not configured") };
        var revision = DrawingDataRowsTests.Revision("rev-1", "dwg-1", "A", DateTimeOffset.UtcNow);

        await AutoQueue(context, queue).QueueLandedRevisionAsync(revision, "proj-1", "pm@jewel.test", CancellationToken.None);

        var row = await context.DrawingExtractions.SingleAsync();
        Assert.Equal((int)DrawingExtractionStatus.Failed, row.Status);
        Assert.Equal(DrawingExtractionAutoQueue.NotQueuedMessage, row.ErrorMessage);
    }

    [Fact]
    public async Task ANonPdf_isSkippedSilently()
    {
        await using var context = NewContext();
        var queue = new RecordingQueue();
        var revision = DrawingDataRowsTests.Revision("rev-1", "dwg-1", "A", DateTimeOffset.UtcNow);
        revision.FileName = "site-photo.jpg";
        revision.ContentType = "image/jpeg";

        await AutoQueue(context, queue).QueueLandedRevisionAsync(revision, "proj-1", "pm@jewel.test", CancellationToken.None);

        Assert.Empty(await context.DrawingExtractions.ToListAsync());
        Assert.Empty(queue.Sent);
    }

    private static DrawingExtractionAutoQueue AutoQueue(JpmsContext context, IDrawingExtractionQueue queue) =>
        new(context, queue, NullLogger<DrawingExtractionAutoQueue>.Instance);

    private static JpmsContext NewContext() =>
        new(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"auto-queue-{Guid.NewGuid():N}").Options);

    private sealed class RecordingQueue : IDrawingExtractionQueue
    {
        public List<DrawingExtractionMessage> Sent { get; } = new();
        public Exception? Failure { get; init; }

        public Task EnqueueAsync(DrawingExtractionMessage message, CancellationToken cancellationToken)
        {
            if (Failure is not null) throw Failure;
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }
}
