namespace Jewel.JPMS.Models;

// A record-type-agnostic projection of a record that an email can be linked to. Any first-class
// record (a Request today, a Bid Package Invite next, an Invoice later) produces one of these so the
// triage "link to existing" panel can list, match and pick it without knowing the concrete entity.
//
// The link itself is unchanged: stamping the Outlook category "JPMS/<TagReference>" on the email is
// the association, and the record reads its mail back live by that same tag. So the only fields the
// link layer needs are the type, the id, the project, and the tag stem — plus a little display text.
public sealed record LinkableRecord(
    RecordType Type,          // Request | BidPackageInvite | …
    string     RecordId,      // the concrete SQL entity's primary key
    string     ProjectId,
    string     Reference,     // human reference shown in the list, e.g. "RFI-001" / "BPI-0001"
    string     TagReference,  // canonical tag stem → "JPMS/<TagReference>" (usually == Reference)
    string     Title,
    string?    StatusLabel = null); // optional small badge, type-specific (e.g. "Open", "Draft")
