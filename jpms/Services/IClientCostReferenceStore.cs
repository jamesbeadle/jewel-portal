
namespace Jewel.JPMS.Services;

/// <summary>
/// The project's cost centre → client schedule-of-works reference map. Small, edited as a
/// whole in one dialog and read only when that dialog opens, so there is no read model to
/// keep warm: every call goes to the server.
/// </summary>
public interface IClientCostReferenceStore
{
    Task<IReadOnlyList<ClientCostReference>> ListAsync(string projectId);

    /// <summary>Replaces the whole map — blank references drop their row.</summary>
    Task<IReadOnlyList<ClientCostReference>> SaveAsync(SetClientCostReferences command);
}
