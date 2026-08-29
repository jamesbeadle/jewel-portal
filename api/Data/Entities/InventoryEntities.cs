using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// An inventory item on a project — a product held for the job and where it's kept. Raised on the
// project's Inventory tab or from a supplier email in the Control Centre (the Supplier pathway's
// "create new"); either way its INV-#### reference is also its mailbox tag stem, so its emails
// read back live by tag — the same arrangement as defects.
public sealed class InventoryItemEntity
{
    [Key, MaxLength(64)] public string InventoryItemId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string ProductName { get; set; } = "";
    [MaxLength(2048)]    public string ProductDetails { get; set; } = "";
    [MaxLength(256)]     public string Location { get; set; } = "";
    [MaxLength(2048)]    public string LocationDetails { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    // Sequential, human-readable item number (rendered as INV-0001). Global — like defect and
    // to-do numbers — so the tag stem is unique across the flat JPMS mailbox-category space.
    // Minted by AddInventoryItemHandler.
    public int Number { get; set; }

    // The canonical reference this item's emails are tagged with ("INV-0001" -> "JPMS/INV-0001").
    // Computed, not stored. The id-derived fallback covers any unnumbered row (there should be
    // none) so two such rows can never share the "INV-0000" stem.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Reference => Number > 0
        ? $"INV-{Number:0000}"
        : $"INV-{InventoryItemId.PadRight(8, '0')[..8].ToUpperInvariant()}";
}
