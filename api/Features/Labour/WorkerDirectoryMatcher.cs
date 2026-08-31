namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>
/// The ONE name-matching rule for joining labour names across systems (2026-08-31, extracted from
/// LabourSupplierRecognition so the Xero import's auto-link and the reconcile sweep cannot drift
/// from what the allocation page's recognition already matches): normalised equality first, then
/// containment either way when the shorter name still carries at least two words
/// ("Pranas Jancauskas Ltd" ⊃ "Pranas Jancauskas"; a lone "Pranas" claims nothing).
/// </summary>
public static class WorkerDirectoryMatcher
{
    public static bool Matches(string a, string b)
    {
        var left = Normalise(a);
        var right = Normalise(b);
        if (left.Length == 0 || right.Length == 0) return false;
        return left == right || ContainsEitherWay(left, right);
    }

    /// <summary>Containment either way over ALREADY-normalised names; the shorter must carry at
    /// least two words.</summary>
    public static bool ContainsEitherWay(string supplier, string worker)
    {
        if (supplier.Length == worker.Length) return false; // equality is tested separately
        var (longer, shorter) = supplier.Length > worker.Length ? (supplier, worker) : (worker, supplier);
        if (!shorter.Contains(' ')) return false;
        return longer.Contains(shorter, StringComparison.Ordinal);
    }

    /// <summary>Lowercase, letters and digits only, single spaces — punctuation and casing
    /// differences between Dext, Xero and the registry must not defeat a match a human would
    /// make instantly.</summary>
    public static string Normalise(string value)
    {
        Span<char> buffer = stackalloc char[value.Length];
        var length = 0;
        var lastWasSpace = true;
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                buffer[length++] = char.ToLowerInvariant(ch);
                lastWasSpace = false;
            }
            else if (!lastWasSpace)
            {
                buffer[length++] = ' ';
                lastWasSpace = true;
            }
        }
        return new string(buffer[..length]).TrimEnd();
    }
}

/// <summary>
/// The settlement counterparty rule (2026-08-31): a worker settles through their linked
/// subcontractor company, or through THEMSELF when flagged a sole trader — the company link wins
/// where both are set. Null means the worker has no settlement identity yet and cannot be
/// reconciled against Xero.
/// </summary>
public static class WorkerSettlementIdentity
{
    public static string? CounterpartyId(string? subcontractorId, bool isSoleTrader, string workerId) =>
        subcontractorId ?? (isSoleTrader ? workerId : null);
}
