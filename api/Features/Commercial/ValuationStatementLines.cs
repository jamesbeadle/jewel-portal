using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial;

/// <summary>
/// The ONE place a valuation becomes a statement (2026-09-18: the claim IS the statement — the
/// separate valuation-report snapshot is gone).
///
/// <see cref="ComputeAsync"/> is the working copy: every priced line of the live bill with the
/// claim's % complete / money (missing entries count as 0%), "this period" derived by the one
/// rule (<see cref="ClaimPeriodBaseline"/>) and the footer with "Certified to date" from the
/// Issued+Paid invoices that came BEFORE the claim (<see cref="CertifiedBeforeClaim"/>). It
/// touches nothing.
///
/// <see cref="FreezeAsync"/> is the lock: the same lines written onto the claim's own rows —
/// the bill line copied by value (description, code, quantity, rate, amount, client reference,
/// statement order) beside the money — so what the client is sent survives every later edit or
/// deletion of the live bill. A row is added for every bill line the claim had no entry for
/// (0%), and every row's stored "this period" is re-stated by the rule at that moment. Adds to
/// the change tracker; the caller saves.
///
/// <see cref="ReadAsync"/> is what every reader calls: a locked claim's own rows, a Draft's
/// working copy — so the viewer, the PDF, the spreadsheet, the emailed attachment and the
/// connector can never disagree about what the statement says.
/// </summary>
internal static class ValuationStatementLines
{
    /// <summary>The claim's statement: its frozen rows when locked, else the working copy.</summary>
    public static async Task<ValuationStatement> ReadAsync(
        JpmsContext context, ValuationClaimEntity claim, CancellationToken cancellationToken)
    {
        if (claim.Status == (int)ValuationClaimStatus.Draft)
            return await ComputeAsync(context, claim, cancellationToken);

        var rows = await context.ClaimLines.AsNoTracking()
            .Where(row => row.ValuationClaimId == claim.ValuationClaimId)
            .OrderBy(row => row.DisplayOrder)
            .ToListAsync(cancellationToken);
        return new ValuationStatement(
            claim.ToModel(),
            rows.Select(row => row.ToStatementLine()).ToList(),
            IsDraft: false,
            AsAt: claim.LockedAt ?? claim.PreapprovedAt ?? claim.ConfirmedAt ?? DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Freezes the claim's statement onto its own rows and stamps LockedAt. Idempotent in effect:
    /// re-locking a reopened claim re-copies the bill as it stands now. The footer is written
    /// separately by <see cref="ValuationClaimSummary.ApplyTotalsAsync"/> (same figures — both
    /// read the same rows).
    /// </summary>
    public static async Task FreezeAsync(
        JpmsContext context, ValuationClaimEntity claim, CancellationToken cancellationToken)
    {
        var lines = await context.ValuationLineItems.AsNoTracking()
            .Where(line => line.ProjectId == claim.ProjectId)
            .ToListAsync(cancellationToken);
        var clientReferencesByCostCode = await ClientReferencesByCostCodeAsync(context, claim.ProjectId, cancellationToken);
        var previousByLine = await ClaimPeriodBaseline.PreviousCumulativeByLineAsync(
            context, claim.ProjectId, claim.ClaimNumber, cancellationToken);

        var rows = await context.ClaimLines
            .Where(row => row.ValuationClaimId == claim.ValuationClaimId)
            .ToListAsync(cancellationToken);
        var rowsByLine = rows.ToDictionary(row => row.ValuationLineItemId);

        var displayOrder = 0;
        foreach (var line in InStatementOrder(lines))
        {
            if (!rowsByLine.TryGetValue(line.ValuationLineItemId, out var row))
            {
                row = new ClaimLineEntity
                {
                    ClaimLineId = CommercialIdentifierFactory.NextClaimLineId(),
                    ValuationClaimId = claim.ValuationClaimId,
                    ValuationLineItemId = line.ValuationLineItemId,
                    PercentComplete = 0m,
                    CumulativeClaimed = 0m
                };
                context.ClaimLines.Add(row);
                rowsByLine[line.ValuationLineItemId] = row;
            }
            CopyBillLine(row, line, clientReferencesByCostCode);
            row.DisplayOrder = displayOrder++;
            row.PeriodIncrement = ClaimPeriodBaseline.PeriodIncrement(
                row.CumulativeClaimed, previousByLine, line.ValuationLineItemId);
        }

        // Rows naming a bill line that no longer exists (none on a Draft in practice — draft rows
        // go with their line) keep their money and trail the statement.
        foreach (var orphan in rows.Where(row => row.DisplayOrder < 0))
            orphan.DisplayOrder = displayOrder++;

        claim.LockedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// The bill line copied by value onto a locked claim's row — at lock, and again for the
    /// lines a value-neutral variation re-breakdown re-deals underneath a locked claim
    /// (VariationClaimRespread: a claim locks its money, never the shape beneath it).
    /// </summary>
    public static void CopyBillLine(
        ClaimLineEntity row, ValuationLineItemEntity line, IReadOnlyDictionary<string, string> clientReferencesByCostCode)
    {
        row.ElementType = line.ElementType;
        row.SectionCode = line.SectionCode;
        row.SectionName = line.SectionName;
        row.VariationRef = line.VariationRef;
        row.VariationTitle = line.VariationTitle;
        row.LineType = line.LineType;
        row.CostCode = line.CostCode;
        row.Description = line.Description;
        row.Unit = line.Unit;
        row.Quantity = line.Quantity;
        row.Rate = line.Rate;
        row.LineAmount = line.LineAmount;
        row.Comments = line.Comments;
        row.ClientReference = !string.IsNullOrWhiteSpace(line.ClientReference)
            ? line.ClientReference
            : clientReferencesByCostCode.GetValueOrDefault(line.CostCode, "");
    }

    /// <summary>
    /// The statement a lock WOULD freeze right now, computed from the live bill and the claim's
    /// entries — the working copy. Nothing persists; the claim model returned carries the
    /// computed footer (a Draft's stored totals are zero).
    /// </summary>
    public static async Task<ValuationStatement> ComputeAsync(
        JpmsContext context, ValuationClaimEntity claim, CancellationToken cancellationToken)
    {
        var lines = await context.ValuationLineItems.AsNoTracking()
            .Where(line => line.ProjectId == claim.ProjectId)
            .ToListAsync(cancellationToken);
        var clientReferencesByCostCode = await ClientReferencesByCostCodeAsync(context, claim.ProjectId, cancellationToken);

        var entriesByLineItem = await context.ClaimLines.AsNoTracking()
            .Where(entry => entry.ValuationClaimId == claim.ValuationClaimId)
            .ToDictionaryAsync(entry => entry.ValuationLineItemId, cancellationToken);

        // "Previous" and "this period" derive from the claim immediately before, by the one rule
        // — never the entry's stored increment, a convenience written when the % was entered.
        var previousByLine = await ClaimPeriodBaseline.PreviousCumulativeByLineAsync(
            context, claim.ProjectId, claim.ClaimNumber, cancellationToken);

        // Gross certification of the invoices that came BEFORE this claim — never its own.
        var certification = await CertifiedBeforeClaim.ForAsync(
            context, claim.ProjectId, claim.ClaimNumber, cancellationToken);

        var lineModels = lines.Select(line => line.ToModel()).ToList();
        var contractSum = ValuationCalculations.ContractSum(lineModels);
        var netVariations = ValuationCalculations.NetVariations(lineModels);

        var statementLines = new List<ValuationStatementLine>(lines.Count);
        var totalWorksComplete = 0m;
        var nonVariationWorksComplete = 0m; // contract-side works — the base the deposit releases against
        var displayOrder = 0;
        foreach (var line in InStatementOrder(lines))
        {
            entriesByLineItem.TryGetValue(line.ValuationLineItemId, out var entry);
            var cumulative = entry?.CumulativeClaimed ?? 0m;
            var statementLine = new ValuationStatementLine(
                claim.ValuationClaimId,
                line.ValuationLineItemId,
                (ValuationElementType)line.ElementType,
                line.SectionCode, line.SectionName,
                line.VariationRef, line.VariationTitle,
                (ValuationLineType)line.LineType,
                line.CostCode, line.Description, line.Unit,
                line.Quantity, line.Rate, line.LineAmount,
                entry?.PercentComplete ?? 0m,
                cumulative,
                ClaimPeriodBaseline.PeriodIncrement(cumulative, previousByLine, line.ValuationLineItemId),
                line.Comments,
                displayOrder++,
                !string.IsNullOrWhiteSpace(line.ClientReference)
                    ? line.ClientReference
                    : clientReferencesByCostCode.GetValueOrDefault(line.CostCode, ""));
            // Declined/TBC lines are recorded but never priced into totals.
            if (statementLine.CountsTowardTotals)
            {
                totalWorksComplete += cumulative;
                if (statementLine.ElementType != ValuationElementType.Variation)
                    nonVariationWorksComplete += cumulative;
            }
            statementLines.Add(statementLine);
        }

        var retentionHeld = ValuationCalculations.RetentionHeld(totalWorksComplete, claim.RetentionPercent);
        var retentionReleased = ValuationCalculations.RetentionReleased(totalWorksComplete, claim.RetentionReleasePercent);
        // Cash-up-front deposit credit still to be taken — mirrors ValuationClaimSummary.
        var depositReleased = ValuationCalculations.DepositDeduction(
            ValuationCalculations.DepositReleased(
                nonVariationWorksComplete, claim.DepositPercent,
                ValuationCalculations.DepositReceived(contractSum, claim.DepositPercent)),
            claim.DepositReleasedOpening, certification.DepositCreditedToDate);

        var footer = claim.ToModel() with
        {
            ContractSum = contractSum,
            NetVariations = netVariations,
            RevisedContractSum = ValuationCalculations.RevisedContractSum(contractSum, netVariations),
            TotalWorksComplete = totalWorksComplete,
            RetentionHeld = retentionHeld,
            RetentionReleased = retentionReleased,
            DepositReleased = depositReleased,
            CertifiedToDate = certification.CertifiedToDate,
            PaymentDueExVat = ValuationCalculations.PaymentDueExVat(
                totalWorksComplete, retentionHeld, retentionReleased, depositReleased, certification.CertifiedToDate)
        };

        return new ValuationStatement(footer, statementLines, IsDraft: true, AsAt: DateTimeOffset.UtcNow);
    }

    // Statement order: element, then variations by their V-number (natural numeric order — a
    // line added to an earlier variation later on must not drop to the bottom), then bill order.
    // Matches the live report table.
    private static IEnumerable<ValuationLineItemEntity> InStatementOrder(IEnumerable<ValuationLineItemEntity> lines)
    {
        static int VariationRefOrder(string variationRef)
        {
            var digits = new string(variationRef.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var number) ? number : int.MaxValue;
        }
        return lines
            .OrderBy(line => line.ElementType)
            .ThenBy(line => line.ElementType == (int)ValuationElementType.Variation ? VariationRefOrder(line.VariationRef) : 0)
            .ThenBy(line => line.DisplayOrder);
    }

    // Grouped rather than ToDictionary so a code duplicated by case alone can never turn a
    // lock (or a working-copy download) into a 500.
    internal static async Task<Dictionary<string, string>> ClientReferencesByCostCodeAsync(
        JpmsContext context, string projectId, CancellationToken cancellationToken)
    {
        var references = await context.ClientCostReferences.AsNoTracking()
            .Where(reference => reference.ProjectId == projectId)
            .Select(reference => new { reference.CostCode, reference.ClientReference })
            .ToListAsync(cancellationToken);
        return references
            .GroupBy(reference => reference.CostCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().ClientReference, StringComparer.OrdinalIgnoreCase);
    }
}
