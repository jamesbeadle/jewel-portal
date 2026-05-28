using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Drawings;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpDrawingStore : IDrawingStore
{
    private readonly DrawingsReadModel readModel;
    private readonly ICommandSender commands;

    public HttpDrawingStore(DrawingsReadModel readModel, ICommandSender commands)
    {
        this.readModel = readModel;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<Drawing> DrawingsFor(string projectId)
    {
        if (readModel.DrawingsCurrent(projectId).Count == 0) _ = readModel.RefreshDrawingsAsync(projectId, CancellationToken.None);
        return readModel.DrawingsCurrent(projectId);
    }

    public Drawing? Find(string drawingId) => null;

    public Drawing Upsert(Drawing drawing)
    {
        if (string.IsNullOrEmpty(drawing.DrawingId)) _ = RegisterAsync(drawing);
        else _ = UpdateAsync(drawing);
        return drawing;
    }

    public IReadOnlyList<DrawingRevision> RevisionsFor(string drawingId)
    {
        if (readModel.RevisionsCurrent(drawingId).Count == 0) _ = readModel.RefreshRevisionsAsync(drawingId, CancellationToken.None);
        return readModel.RevisionsCurrent(drawingId);
    }

    public DrawingRevision AddRevision(DrawingRevision revision)
    {
        _ = IssueAsync(revision);
        return revision;
    }

    public IReadOnlyList<DrawingRevision> AmbiguousFor(string projectId) =>
        DrawingsFor(projectId)
            .SelectMany(drawing => RevisionsFor(drawing.DrawingId))
            .Where(revision => revision.IsAmbiguous)
            .ToList()
            .AsReadOnly();

    private async Task RegisterAsync(Drawing drawing)
    {
        await commands.SendAsync(
            new RegisterDrawing(drawing.ProjectId, drawing.DrawingCode, drawing.Title, drawing.CurrentRevision),
            CancellationToken.None);
        await readModel.RefreshDrawingsAsync(drawing.ProjectId, CancellationToken.None);
    }

    private async Task UpdateAsync(Drawing drawing)
    {
        await commands.SendAsync(
            new UpdateDrawingMetadata(drawing.DrawingId, drawing.DrawingCode, drawing.Title),
            CancellationToken.None);
        await readModel.RefreshDrawingsAsync(drawing.ProjectId, CancellationToken.None);
    }

    private async Task IssueAsync(DrawingRevision revision)
    {
        await commands.SendAsync(
            new IssueDrawingRevision(revision.DrawingId, revision.RevisionLabel, revision.FileName, revision.IssuedByEmail),
            CancellationToken.None);
        await readModel.RefreshRevisionsAsync(revision.DrawingId, CancellationToken.None);
    }
}
