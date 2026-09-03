namespace Jewel.JPMS.Models;

// A site instruction on a project — a written instruction to site: what is to be done (the
// instruction itself, in words), where on site it applies, under a short title. Raised from the
// Control Centre's Internal pane (an email seldom IS the instruction — the triager writes it
// while filing the email that prompted it, 2026-09-03) or on the project's Site Instructions
// page; carries a sequential SI-#### reference, which is also its mailbox tag stem, so every
// email filed to the instruction reads back live under it — the same mechanism as defects,
// to-dos and inventory.
public sealed record SiteInstruction(
    string SiteInstructionId,
    string ProjectId,
    string Title,
    string Instruction,
    string Location,
    DateTimeOffset CreatedAt,
    // Sequential human reference ("SI-0001") — also the mailbox tag stem ("JPMS/SI-0001").
    // Defaulted last so existing construction sites keep compiling; the server always mints it.
    string Reference = "");
