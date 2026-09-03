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
    // The project register was renamed Drawings → Documents on 2026-09-03 (it holds party-wall
    // awards, building-control letters and reports as well as drawings). The action NAMES follow
    // the register; the command parameters (drawingId, drawingFolderId, drawingCode) keep the
    // contract names — AiLegacyNames maps the old action names so saved skills still resolve.
    private static IEnumerable<AiAction> DrawingsActions() => new AiAction[]
    {
        new AiAction(
            Name: "register_document",
            Area: "Documents",
            Description: "Registers a new document on a project's Documents register — a drawing, a "
                + "party-wall award, a building-control letter, a report (code and title, optionally "
                + "inside a folder). Revisions (the files) are uploaded separately through the "
                + "portal.",
            CommandType: typeof(RegisterDrawing),
            ResultType: typeof(Drawing),
            AuthorisationType: typeof(RegisterDrawingAuthorisation),
            ValidationType: typeof(RegisterDrawingValidation),
            VisibleTo: DrawingRegisterCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "drawingFolderId (the parameter keeps the register's old name) is optional — omit "
                + "it to register at the project root."),

        new AiAction(
            Name: "update_document_metadata",
            Area: "Documents",
            Description: "Updates a document's code and title on the project's Documents register. "
                + "Its revisions and files are untouched.",
            CommandType: typeof(UpdateDrawingMetadata),
            ResultType: typeof(Drawing),
            AuthorisationType: typeof(UpdateDrawingMetadataAuthorisation),
            ValidationType: typeof(UpdateDrawingMetadataValidation),
            VisibleTo: DrawingEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "approve_document_revision",
            Area: "Documents",
            Description: "Approves a document revision — it becomes the document's single Approved "
                + "(latest) revision, EVERY other revision of that document is archived, and the "
                + "document's current approved label is set. Recorded as approved by the signed-in "
                + "user.",
            CommandType: typeof(ApproveDrawingRevision),
            ResultType: typeof(DrawingRevision),
            AuthorisationType: typeof(ApproveDrawingRevisionAuthorisation),
            ValidationType: typeof(ApproveDrawingRevisionValidation),
            VisibleTo: DrawingManagers,
            EmailStamps: new[] { "ApprovedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user which revision, by document code and revision label, "
                + "before calling — the previous approved revision is archived."),

        new AiAction(
            Name: "set_document_revision_label",
            Area: "Documents",
            Description: "Sets or clears a document revision's label (e.g. \"P3\", \"C1\").",
            CommandType: typeof(SetDrawingRevisionLabel),
            ResultType: typeof(DrawingRevision),
            AuthorisationType: typeof(SetDrawingRevisionLabelAuthorisation),
            ValidationType: typeof(SetDrawingRevisionLabelValidation),
            VisibleTo: DrawingEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "A blank revisionLabel clears the label."),

        new AiAction(
            Name: "delete_document_revision",
            Area: "Documents",
            Description: "Deletes one document revision permanently, including its stored file. "
                + "There is no undo.",
            CommandType: typeof(DeleteDrawingRevision),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteDrawingRevisionAuthorisation),
            ValidationType: typeof(DeleteDrawingRevisionValidation),
            VisibleTo: DrawingManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which revision, by document code and label, before "
                + "calling."),

        new AiAction(
            Name: "delete_document",
            Area: "Documents",
            Description: "Deletes a document permanently, together with ALL of its revisions, "
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
            Notes: "Confirm with the user which document, by code and title, before calling."),

        new AiAction(
            Name: "create_document_folder",
            Area: "Documents",
            Description: "Creates a folder on a project's Documents register, optionally inside a "
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
            Name: "rename_document_folder",
            Area: "Documents",
            Description: "Renames a folder on the project's Documents register. Its contents are untouched.",
            CommandType: typeof(RenameDrawingFolder),
            ResultType: typeof(DrawingFolder),
            AuthorisationType: typeof(RenameDrawingFolderAuthorisation),
            ValidationType: typeof(RenameDrawingFolderValidation),
            VisibleTo: DrawingRegisterCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "delete_document_folder",
            Area: "Documents",
            Description: "Deletes a folder from the project's Documents register. Business-rule refusals "
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
            Name: "move_document_to_folder",
            Area: "Documents",
            Description: "Moves a document into a folder on the same project's Documents register, or "
                + "to the register root with a null drawingFolderId.",
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
