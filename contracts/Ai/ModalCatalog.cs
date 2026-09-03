using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Ai;

public static partial class ModalCatalog
{
    public static IReadOnlyList<ModalDescriptor> All { get; } =
        new[]
        {
            VariationDraft, ManualVariation, ComposeEmail, ReplyEmail, BidPackageDetails, TenderReply,
            ManualTimesheet, RecordAbsence, WorkerWeek, WorkOrderEdit, WorkOrderCreate,
            VariationEditLines, ClaimProgress, VariationBuildUp
        };

    public static ModalDescriptor? Find(string? modalKey) =>
        string.IsNullOrWhiteSpace(modalKey)
            ? null
            : All.FirstOrDefault(modal =>
                string.Equals(modal.ModalKey, modalKey, StringComparison.OrdinalIgnoreCase));

    /// <summary>The dialogs a set of roles may open. Admin passes everything, as it does everywhere
    /// else (SignedInUserResolver grants administrators all roles).</summary>
    public static IReadOnlyList<ModalDescriptor> For(IEnumerable<Role> roles)
    {
        var held = roles as IReadOnlyCollection<Role> ?? roles.ToList();
        return All.Where(modal => CanOpen(modal, held)).ToList();
    }

    public static bool CanOpen(ModalDescriptor modal, IEnumerable<Role> roles) =>
        roles.Any(role => role == Role.Admin || modal.OpenableBy.Contains(role));

    /// <summary>
    /// The dialog's fields as a JSON Schema object, for a tool's input schema. Mirrors the shape
    /// AiToolSchema.Object produces so the two are interchangeable to the Anthropic API.
    /// </summary>
    public static object SchemaFor(ModalDescriptor modal) => BuildObjectSchema(modal.Fields);

    private static object BuildObjectSchema(IReadOnlyList<ModalField> fields)
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var field in fields)
        {
            properties[field.Name] = field.ItemFields is { Count: > 0 }
                ? new
                {
                    type = field.Type,
                    description = field.Description,
                    items = BuildObjectSchema(field.ItemFields)
                }
                : (object)new { type = field.Type, description = field.Description };

            if (field.Required) required.Add(field.Name);
        }

        return new
        {
            type = "object",
            properties,
            required = required.ToArray()
        };
    }
}
