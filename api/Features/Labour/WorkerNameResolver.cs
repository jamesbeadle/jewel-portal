using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>Resolves a worker by NAME against the register, for the connector-shaped by-name
/// commands (SubmitWorkerWeekByName, RecordWorkerAbsenceByName): an AI caller meets workers as
/// names, never as the register's opaque ids. Exact normalised match first, then containment
/// either way; every failure throws an InvalidOperationException whose message tells the model
/// exactly what to do next (the executor returns these as answers, not 500s).
/// <paramref name="activityPhrase"/> finishes the inactive-worker sentence ("… before
/// logging time against them" / "… before recording an absence against them").</summary>
internal static class WorkerNameResolver
{
    public static WorkerEntity Resolve(IReadOnlyList<WorkerEntity> workers, string workerName, string activityPhrase)
    {
        var wanted = Normalise(workerName);
        var active = workers.Where(worker => worker.IsActive).ToList();

        var matches = active.Where(worker => Normalise(worker.Name) == wanted).ToList();
        if (matches.Count == 0)
            matches = active.Where(worker => Normalise(worker.Name).Contains(wanted)
                                             || wanted.Contains(Normalise(worker.Name))).ToList();

        if (matches.Count == 1) return matches[0];

        if (matches.Count > 1)
            throw new InvalidOperationException(
                $"\"{workerName}\" matches more than one worker on the register: "
                + string.Join(", ", matches.Select(worker => worker.Name))
                + ". Use the full name as the register spells it.");

        var inactive = workers.FirstOrDefault(worker =>
            !worker.IsActive && Normalise(worker.Name) == wanted);
        if (inactive is not null)
            throw new InvalidOperationException(
                $"{inactive.Name} is on the register but marked inactive — reactivate them on the "
                + $"Workers page before {activityPhrase}.");

        throw new InvalidOperationException(
            $"No worker called \"{workerName}\" is on the register. "
            + (active.Count == 0
                ? "The register has no active workers yet — add them with add_worker or on the "
                  + "Workers page first."
                : "Active workers: "
                  + string.Join(", ", active.OrderBy(worker => worker.Name).Select(worker => worker.Name))
                  + ". Add anyone missing with add_worker or on the Workers page (only a name and "
                  + "hourly rate are needed — no email)."));
    }

    private static string Normalise(string name) =>
        string.Join(' ', name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
}
