using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

/// <summary>
/// The drawing-folder commands, one route each: create under the project, rename/delete on the
/// folder, and move-drawing on the drawing. One class because the plumbing is identical — resolve
/// the user, authorise, validate, run the handler, surface InvalidOperationException (not found,
/// duplicate name, cross-project move) as a 400 the calling dialog shows verbatim.
/// </summary>
public sealed class DrawingFolderCommandEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly CreateDrawingFolderAuthorisation createAuthorisation;
    private readonly CreateDrawingFolderValidation createValidation;
    private readonly ICommandHandler<CreateDrawingFolder, DrawingFolder> createHandler;
    private readonly RenameDrawingFolderAuthorisation renameAuthorisation;
    private readonly RenameDrawingFolderValidation renameValidation;
    private readonly ICommandHandler<RenameDrawingFolder, DrawingFolder> renameHandler;
    private readonly DeleteDrawingFolderAuthorisation deleteAuthorisation;
    private readonly DeleteDrawingFolderValidation deleteValidation;
    private readonly ICommandHandler<DeleteDrawingFolder, Acknowledgement> deleteHandler;
    private readonly MoveDrawingToFolderAuthorisation moveAuthorisation;
    private readonly MoveDrawingToFolderValidation moveValidation;
    private readonly ICommandHandler<MoveDrawingToFolder, Drawing> moveHandler;

    public DrawingFolderCommandEndpoints(
        SignedInUserResolver users,
        CreateDrawingFolderAuthorisation createAuthorisation,
        CreateDrawingFolderValidation createValidation,
        ICommandHandler<CreateDrawingFolder, DrawingFolder> createHandler,
        RenameDrawingFolderAuthorisation renameAuthorisation,
        RenameDrawingFolderValidation renameValidation,
        ICommandHandler<RenameDrawingFolder, DrawingFolder> renameHandler,
        DeleteDrawingFolderAuthorisation deleteAuthorisation,
        DeleteDrawingFolderValidation deleteValidation,
        ICommandHandler<DeleteDrawingFolder, Acknowledgement> deleteHandler,
        MoveDrawingToFolderAuthorisation moveAuthorisation,
        MoveDrawingToFolderValidation moveValidation,
        ICommandHandler<MoveDrawingToFolder, Drawing> moveHandler)
    {
        this.users = users;
        this.createAuthorisation = createAuthorisation;
        this.createValidation = createValidation;
        this.createHandler = createHandler;
        this.renameAuthorisation = renameAuthorisation;
        this.renameValidation = renameValidation;
        this.renameHandler = renameHandler;
        this.deleteAuthorisation = deleteAuthorisation;
        this.deleteValidation = deleteValidation;
        this.deleteHandler = deleteHandler;
        this.moveAuthorisation = moveAuthorisation;
        this.moveValidation = moveValidation;
        this.moveHandler = moveHandler;
    }

    [Function(nameof(CreateDrawingFolder))]
    public async Task<IActionResult> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/drawing-folders")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<CreateDrawingFolder>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!createAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = createValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            var folder = await createHandler.HandleAsync(command, request.HttpContext.RequestAborted);
            return new OkObjectResult(folder);
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }

    [Function(nameof(RenameDrawingFolder))]
    public async Task<IActionResult> Rename(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "drawing-folders/{folderId}")] HttpRequest request,
        string folderId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<RenameDrawingFolder>();
        if (command is null) return new BadRequestResult();
        if (command.DrawingFolderId != folderId) return new BadRequestObjectResult("Route folderId does not match body.");

        if (!renameAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = renameValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            var folder = await renameHandler.HandleAsync(command, request.HttpContext.RequestAborted);
            return new OkObjectResult(folder);
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }

    [Function(nameof(DeleteDrawingFolder))]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "drawing-folders/{folderId}")] HttpRequest request,
        string folderId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new DeleteDrawingFolder(folderId);

        if (!deleteAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = deleteValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            var acknowledgement = await deleteHandler.HandleAsync(command, request.HttpContext.RequestAborted);
            return new OkObjectResult(acknowledgement);
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }

    [Function(nameof(MoveDrawingToFolder))]
    public async Task<IActionResult> Move(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "drawings/{drawingId}/folder")] HttpRequest request,
        string drawingId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<MoveDrawingToFolder>();
        if (command is null) return new BadRequestResult();
        if (command.DrawingId != drawingId) return new BadRequestObjectResult("Route drawingId does not match body.");

        if (!moveAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = moveValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            var drawing = await moveHandler.HandleAsync(command, request.HttpContext.RequestAborted);
            return new OkObjectResult(drawing);
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
