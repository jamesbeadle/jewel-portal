using Jewel.JPMS.Api.Features.Closeout.Commands;
using Jewel.JPMS.Api.Features.Drawings.Commands;
using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Api.Features.Progress.Commands;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Site.Commands;
using Jewel.JPMS.Api.Features.Todos;
using Jewel.JPMS.Api.Features.Todos.Commands;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class SiteAndProgressActions
{
    private static IEnumerable<AiAction> DrawingsActions() => new AiAction[]
    {
        new AiAction(
            Name: "register_drawing",
            Area: "Drawings",
            Description: "Registers a new drawing on a project's drawing register (code and "
                + "title, optionally inside a folder). Revisions are uploaded separately through "
                + "the portal.",
            CommandType: typeof(RegisterDrawing),
            ResultType: typeof(Drawing),
            AuthorisationType: typeof(RegisterDrawingAuthorisation),
            ValidationType: typeof(RegisterDrawingValidation),
            VisibleTo: DrawingRegisterCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "drawingFolderId is optional — omit it to register at the project root."),

        new AiAction(
            Name: "update_drawing_metadata",
            Area: "Drawings",
            Description: "Updates a drawing's code and title on the register. Its revisions and "
                + "files are untouched.",
            CommandType: typeof(UpdateDrawingMetadata),
            ResultType: typeof(Drawing),
            AuthorisationType: typeof(UpdateDrawingMetadataAuthorisation),
            ValidationType: typeof(UpdateDrawingMetadataValidation),
            VisibleTo: DrawingEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "approve_drawing_revision",
            Area: "Drawings",
            Description: "Approves a drawing revision — it becomes the drawing's single Approved "
                + "(latest) revision, EVERY other revision of that drawing is archived, and the "
                + "drawing's current approved label is set. Recorded as approved by the signed-in "
                + "user.",
            CommandType: typeof(ApproveDrawingRevision),
            ResultType: typeof(DrawingRevision),
            AuthorisationType: typeof(ApproveDrawingRevisionAuthorisation),
            ValidationType: typeof(ApproveDrawingRevisionValidation),
            VisibleTo: DrawingManagers,
            EmailStamps: new[] { "ApprovedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user which revision, by drawing code and revision label, "
                + "before calling — the previous approved revision is archived."),

        new AiAction(
            Name: "set_drawing_revision_label",
            Area: "Drawings",
            Description: "Sets or clears a drawing revision's label (e.g. \"P3\", \"C1\").",
            CommandType: typeof(SetDrawingRevisionLabel),
            ResultType: typeof(DrawingRevision),
            AuthorisationType: typeof(SetDrawingRevisionLabelAuthorisation),
            ValidationType: typeof(SetDrawingRevisionLabelValidation),
            VisibleTo: DrawingEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "A blank revisionLabel clears the label."),

        new AiAction(
            Name: "delete_drawing_revision",
            Area: "Drawings",
            Description: "Deletes one drawing revision permanently, including its stored file. "
                + "There is no undo.",
            CommandType: typeof(DeleteDrawingRevision),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteDrawingRevisionAuthorisation),
            ValidationType: typeof(DeleteDrawingRevisionValidation),
            VisibleTo: DrawingManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which revision, by drawing code and label, before "
                + "calling."),

        new AiAction(
            Name: "delete_drawing",
            Area: "Drawings",
            Description: "Deletes a drawing permanently, together with ALL of its revisions, "
                + "their stored files and any issue records that referenced them. There is no "
                + "undo.",
            CommandType: typeof(DeleteDrawing),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteDrawingAuthorisation),
            ValidationType: typeof(DeleteDrawingValidation),
            VisibleTo: DrawingManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which drawing, by code and title, before calling."),

        new AiAction(
            Name: "create_drawing_folder",
            Area: "Drawings",
            Description: "Creates a folder on a project's drawing register, optionally inside a "
                + "parent folder.",
            CommandType: typeof(CreateDrawingFolder),
            ResultType: typeof(DrawingFolder),
            AuthorisationType: typeof(CreateDrawingFolderAuthorisation),
            ValidationType: typeof(CreateDrawingFolderValidation),
            VisibleTo: DrawingRegisterCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "A duplicate name among siblings is refused."),

        new AiAction(
            Name: "rename_drawing_folder",
            Area: "Drawings",
            Description: "Renames a drawing folder. Its contents are untouched.",
            CommandType: typeof(RenameDrawingFolder),
            ResultType: typeof(DrawingFolder),
            AuthorisationType: typeof(RenameDrawingFolderAuthorisation),
            ValidationType: typeof(RenameDrawingFolderValidation),
            VisibleTo: DrawingRegisterCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "delete_drawing_folder",
            Area: "Drawings",
            Description: "Deletes a drawing folder from the register. Business-rule refusals "
                + "(e.g. a folder that is not empty) come back as errors rather than deleting "
                + "contents.",
            CommandType: typeof(DeleteDrawingFolder),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteDrawingFolderAuthorisation),
            ValidationType: typeof(DeleteDrawingFolderValidation),
            VisibleTo: DrawingRegisterCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user which folder, by name, before calling."),

        new AiAction(
            Name: "move_drawing_to_folder",
            Area: "Drawings",
            Description: "Moves a drawing into a folder on the same project's register, or to the "
                + "register root with a null drawingFolderId.",
            CommandType: typeof(MoveDrawingToFolder),
            ResultType: typeof(Drawing),
            AuthorisationType: typeof(MoveDrawingToFolderAuthorisation),
            ValidationType: typeof(MoveDrawingToFolderValidation),
            VisibleTo: DrawingRegisterCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Cross-project moves are refused."),

        // ── Closeout & defects ────────────────────────────────────────────────────────────────

    };
}
