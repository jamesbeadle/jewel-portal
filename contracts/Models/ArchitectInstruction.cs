namespace Jewel.JPMS.Models;

/// <summary>How an Architect's Instruction reached the portal.</summary>
public enum ArchitectInstructionSource
{
    /// <summary>Filed directly through the portal by an architect or the Jewel team.</summary>
    Upload = 0,
    /// <summary>Pulled out of a triaged email in the projects mailbox.</summary>
    Email = 1
}

/// <summary>
/// An Architect's Instruction (AI) — the formal written instruction under the building contract
/// that turns "we'd like this changed" into work Jewel is entitled to be paid for. It is the
/// document a variation sitting at <see cref="VariationOrderStatus.AwaitingArchitectInstruction"/>
/// is waiting for, which is why an instruction can be linked to the variations it covers: one AI
/// routinely instructs several, and an AI can land before the variation it will end up justifying.
///
/// Instructions arrive two ways and both land here: emailed to the projects mailbox and imported
/// from the attachment, or uploaded straight into the register. Either way the stored file is the
/// evidence, so <see cref="HasFile"/> being false means the row is a placeholder someone still
/// needs to attach the document to.
/// </summary>
public sealed record ArchitectInstruction(
    string ArchitectInstructionId,
    string ProjectId,
    // JPMS's own sequential reference within the project (AI-0001). Always present.
    string Reference,
    // The architect's own number as written on the instruction ("AI 042", "PLG-AI-17"). Free text,
    // because every practice numbers them differently — and it is the number the client will quote.
    string InstructionRef,
    string Title,
    string? Notes,
    // The date written on the instruction itself — what the contract runs from. Null until known.
    DateTimeOffset? InstructedAt,
    // When the portal received it (upload time, or the import time for an emailed one).
    DateTimeOffset ReceivedAt,
    // The architect who issued it: the email's sender on an import, entered by hand on an upload.
    string IssuedByEmail,
    // Who filed it in the portal. Distinct from IssuedByEmail — usually a Jewel PM.
    string FiledByEmail,
    ArchitectInstructionSource Source,
    string? FileName,
    string? ContentType,
    long? FileSizeBytes,
    bool HasFile,
    // The variations this instruction covers. Null on payloads that don't expand the links.
    IReadOnlyList<ArchitectInstructionVariationLink>? LinkedVariations = null)
{
    /// <summary>What a person reads: the architect's own number when they gave one, else ours.</summary>
    public string DisplayReference =>
        string.IsNullOrWhiteSpace(InstructionRef) ? Reference : InstructionRef.Trim();

    /// <summary>The linked variations, never null.</summary>
    public IReadOnlyList<ArchitectInstructionVariationLink> Links =>
        LinkedVariations ?? Array.Empty<ArchitectInstructionVariationLink>();
}

/// <summary>A variation an instruction covers, denormalised so the register renders without joins.</summary>
public sealed record ArchitectInstructionVariationLink(
    string VariationOrderId,
    string DisplayNumber,
    string Title,
    VariationOrderStatus Status);
