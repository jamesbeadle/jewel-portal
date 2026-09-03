using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Kpi;

/// <summary>
/// Turns "the person a KPI is about" — named as an id, a portal email, or a bare name — into one
/// KpiPersonEntity, creating it when it does not exist yet. Shared by the mark, update and
/// add-person handlers so a person is minted by one rule wherever they first appear: a portal
/// user gets exactly one row (found by email), a name-only person exactly one (found by name,
/// case-insensitive). Adds are saved by the CALLER's SaveChanges.
/// </summary>
public sealed class KpiPersonResolver
{
    private readonly JpmsContext context;
    public KpiPersonResolver(JpmsContext context) { this.context = context; }

    public async Task<KpiPersonEntity> ResolveAsync(string? personId, string? personEmail, string? personName, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(personId))
        {
            var id = personId.Trim();
            return await context.KpiPeople.FirstOrDefaultAsync(row => row.KpiPersonId == id, cancellationToken)
                ?? throw new InvalidOperationException($"No KPI person has the id \"{id}\" — list_kpi_people gives the ids.");
        }

        if (!string.IsNullOrWhiteSpace(personEmail))
            return await ForPortalUserAsync(personEmail.Trim(), cancellationToken);

        if (!string.IsNullOrWhiteSpace(personName))
            return await ForNameAsync(personName.Trim(), cancellationToken);

        throw new InvalidOperationException("Say who the KPI is about: personId, personEmail (a portal user) or personName (someone without a login).");
    }

    /// <summary>The one KpiPerson row for a portal user, created from the directory on first use.
    /// The directory must know the email — a KPI files under staff, not arbitrary addresses.</summary>
    public async Task<KpiPersonEntity> ForPortalUserAsync(string email, CancellationToken cancellationToken)
    {
        var existing = await context.KpiPeople
            .FirstOrDefaultAsync(row => row.Email != null && row.Email == email, cancellationToken);
        if (existing is not null) return existing;

        var user = await context.DirectoryUsers.AsNoTracking()
            .FirstOrDefaultAsync(row => row.Email == email, cancellationToken)
            ?? throw new InvalidOperationException(
                $"No portal user has the email \"{email}\". For someone without a login, give their name instead (personName).");

        var person = new KpiPersonEntity
        {
            KpiPersonId = Guid.NewGuid().ToString("N"),
            Name = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email : user.DisplayName,
            Email = user.Email,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.KpiPeople.Add(person);
        return person;
    }

    /// <summary>A name-only person, matched case-insensitively against everyone already on the
    /// list (portal users included — "James Clark" typed by hand must not twin a James Clark who
    /// has a login), else created.</summary>
    public async Task<KpiPersonEntity> ForNameAsync(string name, CancellationToken cancellationToken)
    {
        var people = await context.KpiPeople.ToListAsync(cancellationToken);
        var existing = people.FirstOrDefault(row => string.Equals(row.Name.Trim(), name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) return existing;

        // A portal user typed by name rather than picked: link them rather than mint a stranger.
        var user = (await context.DirectoryUsers.AsNoTracking()
                .Where(row => row.RevokedAt == null)
                .Select(row => new { row.Email, row.DisplayName })
                .ToListAsync(cancellationToken))
            .FirstOrDefault(row => string.Equals(row.DisplayName.Trim(), name, StringComparison.OrdinalIgnoreCase));

        var person = new KpiPersonEntity
        {
            KpiPersonId = Guid.NewGuid().ToString("N"),
            Name = user is null ? name : (string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email : user.DisplayName),
            Email = user?.Email,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.KpiPeople.Add(person);
        return person;
    }
}
