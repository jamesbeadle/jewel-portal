using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Sales;

/// <summary>
/// Removes a lead from the register for good (2026-09-15) — the lead row and everything hung
/// off it: its timeline, its estimates, its proposals and its imagine rounds and images. A
/// mistaken capture, a duplicate, a test entry. Directors only, and never a Won lead: that has
/// a client and a project behind it, and Lost is its only way out. Emails tagged JPMS/LD-####
/// keep their tag in the mailbox; the tag simply resolves to nothing afterwards. DeletedByEmail
/// is stamped by the server.
/// </summary>
public sealed record DeleteLead(string LeadId, string DeletedByEmail = "") : ICommand<Acknowledgement>;
