using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ArchitectInstructions;

// The Architect's Instruction register. Kept in one file because the whole feature is six small
// messages over one table; splitting them one-per-file would be six files of three lines each.

/// <summary>Every instruction on a project, newest first, with its variation links expanded.</summary>
public sealed record ListArchitectInstructionsForProject(string ProjectId)
    : IQuery<IReadOnlyList<ArchitectInstruction>>;

/// <summary>One instruction with its variation links, or null when it no longer exists.</summary>
public sealed record GetArchitectInstructionById(string ArchitectInstructionId)
    : IQuery<ArchitectInstruction?>;

/// <summary>
/// Records an instruction whose file the endpoint has already streamed to blob storage — the same
/// division of labour as UploadDrawingRevision, so the blob path and the persisted row share an id
/// minted before either exists. BlobRef is null for a placeholder row filed ahead of the document.
/// </summary>
public sealed record RecordArchitectInstruction(
    string ArchitectInstructionId,
    string ProjectId,
    string InstructionRef,
    string Title,
    string? Notes,
    DateTimeOffset? InstructedAt,
    string IssuedByEmail,
    string FiledByEmail,
    ArchitectInstructionSource Source,
    string? FileName = null,
    string? ContentType = null,
    long? FileSizeBytes = null,
    string? BlobRef = null,
    // Optionally link the instruction to variations as it is filed, so an AI that arrives for a
    // variation already sitting at Awaiting AI does not need a second step.
    IReadOnlyList<string>? VariationOrderIds = null) : ICommand<ArchitectInstruction>;

/// <summary>
/// Pulls one attachment out of a triaged email in the projects mailbox and files it as an
/// instruction. IssuedByEmail is taken from the email's sender (the architect), never the triager —
/// the same rule the drawing import follows.
/// </summary>
public sealed record ImportArchitectInstructionFromMessage(
    string ProjectId,
    string MessageId,
    string AttachmentId,
    string InstructionRef,
    string Title,
    DateTimeOffset? InstructedAt = null,
    IReadOnlyList<string>? VariationOrderIds = null) : ICommand<ArchitectInstruction>;

/// <summary>Corrects the details on a filed instruction. The stored file is never touched.</summary>
public sealed record UpdateArchitectInstruction(
    string ArchitectInstructionId,
    string InstructionRef,
    string Title,
    string? Notes,
    DateTimeOffset? InstructedAt) : ICommand<ArchitectInstruction>;

/// <summary>Links an instruction to a variation it covers. Linking twice is a no-op.</summary>
public sealed record LinkArchitectInstructionToVariation(
    string ArchitectInstructionId,
    string VariationOrderId) : ICommand<ArchitectInstruction>;

/// <summary>Removes a link. The instruction and the variation both survive.</summary>
public sealed record UnlinkArchitectInstructionFromVariation(
    string ArchitectInstructionId,
    string VariationOrderId) : ICommand<ArchitectInstruction>;

/// <summary>Permanently deletes an instruction, its links and its stored file.</summary>
public sealed record DeleteArchitectInstruction(string ArchitectInstructionId)
    : ICommand<Acknowledgement>;
