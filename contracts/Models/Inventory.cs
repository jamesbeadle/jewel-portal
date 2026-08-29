namespace Jewel.JPMS.Models;

// An inventory item on a project — a product held for the job (what it is, where it's kept).
// Raised from a supplier email in the Control Centre or on the project's Inventory tab; carries
// a sequential INV-#### reference, which is also its mailbox tag stem, so every email filed to
// the item reads back live under it — the same mechanism as defects and work orders.
public sealed record InventoryItem(
    string InventoryItemId,
    string ProjectId,
    string ProductName,
    string ProductDetails,
    string Location,
    string LocationDetails,
    DateTimeOffset CreatedAt,
    // Sequential human reference ("INV-0001") — also the mailbox tag stem ("JPMS/INV-0001").
    // Defaulted last so existing construction sites keep compiling; the server always mints it.
    string Reference = "");
