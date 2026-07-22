using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Drafts the work-order email to the supplier in the shared mailbox — nothing is sent; a human
// reviews and sends it from Outlook, matching the tender-invite convention. The recipient is the
// supplier's directory email. When the order came from awarding a bid package the draft carries the
// package's tag, so the sent copy and any replies group under the package's correspondence.
public sealed record PrepareWorkOrderEmailDraft(
    string WorkOrderId,
    string Subject,
    string HtmlBody) : ICommand<WorkOrderEmailDraft>;
