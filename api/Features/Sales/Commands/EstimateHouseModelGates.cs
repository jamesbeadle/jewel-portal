using System.Text.Json;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Commands;

/// <summary>The 3D model write's gates: the sales team's, and at least a named house with a block.</summary>
public sealed class SetEstimateHouseModelAuthorisation
{
    public bool Allows(SignedInUser user, SetEstimateHouseModel command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class SetEstimateHouseModelValidation
{
    public ValidationOutcome Check(SetEstimateHouseModel command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.EstimateId)) errors.Add("EstimateId is required.");
        if (string.IsNullOrWhiteSpace(command.Source)) errors.Add("Say which drawing sheets and revision the model was read from (source).");
        SalesFieldLimits.Check(errors, command.Source, 1024, "Source");
        CheckDefinition(errors, command.Model);
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }

    private static void CheckDefinition(List<string> errors, JsonElement model)
    {
        if (model.ValueKind != JsonValueKind.Object)
        {
            errors.Add("The model must be the definition object — see the jpms-house-model skill.");
            return;
        }
        if (!HouseModelShape.HasName(model)) errors.Add("The model needs a name — the property the house is.");
        if (HouseModelShape.CountOf(model, "blocks") == 0) errors.Add("The model needs at least one block — the house as it stands.");
        if (model.GetRawText().Length > HouseModelShape.MaxJsonCharacters)
            errors.Add($"The model is limited to {HouseModelShape.MaxJsonCharacters:N0} characters of JSON.");
    }
}
