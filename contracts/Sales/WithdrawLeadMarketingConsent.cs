using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Sales;

/// <summary>
/// The prospect has asked Jewel to stop keeping in touch about their home — said on the phone,
/// in a reply, or by the door on their own imagine page. Records the withdrawal on the lead so
/// every follow-up send reads it; the concepts they asked for are unaffected. WithdrawnByEmail is
/// stamped by the server (empty when the prospect did it themselves).
/// </summary>
public sealed record WithdrawLeadMarketingConsent(string LeadId, string WithdrawnByEmail = "") : ICommand<Lead>;
