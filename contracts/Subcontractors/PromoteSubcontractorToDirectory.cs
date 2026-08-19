using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

// Promotes a tender-only prospect (see Subcontractor.IsProspect) into the Directory proper — the
// deliberate "this company is worth keeping" act, offered next to a submitted tender on a bid
// package. Idempotent: promoting a record already in the directory returns it unchanged.
public sealed record PromoteSubcontractorToDirectory(string SubcontractorId) : ICommand<Subcontractor>;
