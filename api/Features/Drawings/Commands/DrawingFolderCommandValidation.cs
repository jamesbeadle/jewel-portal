using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

// Validation for the drawing-folder commands, one class per command as everywhere else, kept in
// one file with its siblings because each is a couple of required-field checks.

public sealed class CreateDrawingFolderValidation
{
    public ValidationOutcome Check(CreateDrawingFolder command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Name)) errors.Add("Folder name is required.");
        if (command.Name?.Trim().Length > 128) errors.Add("Folder name must be 128 characters or fewer.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}

public sealed class RenameDrawingFolderValidation
{
    public ValidationOutcome Check(RenameDrawingFolder command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.DrawingFolderId)) errors.Add("DrawingFolderId is required.");
        if (string.IsNullOrWhiteSpace(command.Name)) errors.Add("Folder name is required.");
        if (command.Name?.Trim().Length > 128) errors.Add("Folder name must be 128 characters or fewer.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}

public sealed class DeleteDrawingFolderValidation
{
    public ValidationOutcome Check(DeleteDrawingFolder command)
    {
        if (string.IsNullOrWhiteSpace(command.DrawingFolderId))
            return new ValidationOutcome(new[] { "DrawingFolderId is required." });
        return ValidationOutcome.Passed;
    }
}

public sealed class MoveDrawingToFolderValidation
{
    public ValidationOutcome Check(MoveDrawingToFolder command)
    {
        // A null DrawingFolderId is a valid move — it ungroups the drawing.
        if (string.IsNullOrWhiteSpace(command.DrawingId))
            return new ValidationOutcome(new[] { "DrawingId is required." });
        return ValidationOutcome.Passed;
    }
}
