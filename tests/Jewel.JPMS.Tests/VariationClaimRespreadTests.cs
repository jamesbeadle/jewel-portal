using Jewel.JPMS.Commercial;
using Xunit;

// Re-shaping a claimed variation's breakdown (edit lines on an approved, claimed variation): a
// claim locks the money it certified for the variation, not the shape of the lines beneath it.
// ClaimMoneySpread deals one certified sum across lines at one percentage, adding back to the
// penny whatever the rounding; ClaimRespread.Plan applies that claim by claim — settled claims keep
// their money, the Draft keeps its percentages, "this period" follows the claim before. These are
// what ReviseVariationOrderLinesHandler / VariationClaimRespread write onto the entities.
public sealed class VariationClaimRespreadTests
{
    // V23 Fluid Glass BWIC on Woodhouse (2026-09-16): one £1,749.78 line, 100% claimed, broken
    // down into the nine-line build-up it was priced from — same total.
    private static readonly (string, decimal)[] NineLines =
    {
        ("phonotherm", 375.90m), ("delivery", 96.00m), ("fixings", 12.96m), ("jamb", 0m),
        ("disc-hire", 63.96m), ("disc", 79.39m), ("labour", 687.50m), ("alterations", 275.00m),
        ("ohp", 159.07m)
    };

    [Fact]
    public void OneLineAt100Percent_becomesEveryLineAt100Percent_andTheSameMoney()
    {
        var spread = ClaimMoneySpread.Spread(1_749.78m, NineLines);

        Assert.Equal(100m, ClaimMoneySpread.Percent(1_749.78m, NineLines.Sum(line => line.Item2)));
        foreach (var (id, amount) in NineLines) Assert.Equal(amount, spread[id]);
        Assert.Equal(1_749.78m, spread.Values.Sum());
    }

    [Fact]
    public void APartlyClaimedSum_isDealtInProportion_andAddsBackToThePenny()
    {
        // 50% of the variation: 79.39 / 2 = 39.695, 63.96 / 2 = 31.98 … the raw shares round
        // individually, and the remainder lands on the largest line so nothing is lost.
        var certified = 874.89m;
        var spread = ClaimMoneySpread.Spread(certified, NineLines);

        Assert.Equal(certified, spread.Values.Sum());
        Assert.Equal(0m, spread["jamb"]);
        Assert.Equal(48.00m, spread["delivery"]);
        foreach (var value in spread.Values) Assert.Equal(Math.Round(value, ClaimMoneySpread.MoneyScale), value);
    }

    [Fact]
    public void TheRoundingRemainder_landsOnTheLargestLine()
    {
        // Three equal lines and £1.00: 0.3333 each leaves £0.0001 over, which the first (largest —
        // all equal, so the first) line carries.
        var spread = ClaimMoneySpread.Spread(1.00m, new[] { ("a", 10m), ("b", 10m), ("c", 10m) });

        Assert.Equal(0.3334m, spread["a"]);
        Assert.Equal(0.3333m, spread["b"]);
        Assert.Equal(0.3333m, spread["c"]);
        Assert.Equal(1.00m, spread.Values.Sum());
    }

    [Fact]
    public void LinesTotallingNothing_keepTheMoneyOnTheFirstLine()
    {
        var spread = ClaimMoneySpread.Spread(500m, new[] { ("add", 250m), ("omit", -250m) });

        Assert.Equal(500m, spread["add"]);
        Assert.Equal(0m, spread["omit"]);
        Assert.Equal(0m, ClaimMoneySpread.Percent(500m, 0m));
    }

    [Fact]
    public void NoLines_spreadsNothing()
    {
        Assert.Empty(ClaimMoneySpread.Spread(500m, Array.Empty<(string, decimal)>()));
    }

    // ---- What the re-spread does claim by claim (ClaimRespread.Plan) --------

    private static readonly ClaimRespread.Claim[] Claims =
    {
        new("c9", IsDraft: false), new("c10", IsDraft: false)
    };

    private static readonly HashSet<string> AddedIds =
        NineLines.Skip(1).Select(line => line.Item1).ToHashSet();

    // The existing single line keeps its id and becomes the first of the nine.
    private static (string, decimal)[] RevisedLines() =>
        NineLines.Select((line, i) => (i == 0 ? "old-line" : line.Item1, line.Item2)).ToArray();

    [Fact]
    public void SettledClaims_keepTheirCertifiedMoney_spreadAcrossTheNewLinesAt100Percent()
    {
        var prior = new[]
        {
            new ClaimRespread.PriorEntry("c9", "old-line", 100m, 1_749.78m),
            new ClaimRespread.PriorEntry("c10", "old-line", 100m, 1_749.78m)
        };

        var plan = ClaimRespread.Plan(Claims, prior, RevisedLines(), AddedIds, oldTotal: 1_749.78m);

        foreach (var claimId in new[] { "c9", "c10" })
        {
            var entries = plan.Where(entry => entry.ValuationClaimId == claimId).ToList();
            Assert.Equal(9, entries.Count);
            Assert.All(entries, entry => Assert.Equal(100m, entry.PercentComplete));
            Assert.Equal(1_749.78m, entries.Sum(entry => entry.CumulativeClaimed));
            Assert.Single(entries, entry => !entry.IsNew && entry.ValuationLineItemId == "old-line");
            Assert.Equal(375.90m, entries.Single(entry => entry.ValuationLineItemId == "old-line").CumulativeClaimed);
        }
        // Claim 9 was the first to carry it, so it claimed the lot; Claim 10 measures from Claim 9's
        // NEW spread — nothing this period on any line, not +£1,373.88 on eight and −£1,373.88 on one.
        Assert.Equal(1_749.78m, plan.Where(entry => entry.ValuationClaimId == "c9").Sum(entry => entry.PeriodIncrement));
        Assert.All(plan.Where(entry => entry.ValuationClaimId == "c10"), entry => Assert.Equal(0m, entry.PeriodIncrement));
    }

    [Fact]
    public void TheDraftClaim_keepsItsPercentageOnTheRepricedLine_andNewLinesJoinAtTheVariationsPercentage()
    {
        var claims = new[] { new ClaimRespread.Claim("c10", IsDraft: false), new ClaimRespread.Claim("c11", IsDraft: true) };
        var prior = new[]
        {
            new ClaimRespread.PriorEntry("c10", "old-line", 50m, 874.89m),
            new ClaimRespread.PriorEntry("c11", "old-line", 100m, 1_749.78m)
        };

        var plan = ClaimRespread.Plan(claims, prior, RevisedLines(), AddedIds, oldTotal: 1_749.78m);
        var draft = plan.Where(entry => entry.ValuationClaimId == "c11").ToList();

        Assert.Equal(9, draft.Count);
        Assert.All(draft, entry => Assert.Equal(100m, entry.PercentComplete));
        Assert.Equal(1_749.78m, draft.Sum(entry => entry.CumulativeClaimed));
        // This period = the other half, measured against Claim 10's re-spread.
        Assert.Equal(874.89m, draft.Sum(entry => entry.PeriodIncrement));
    }

    [Fact]
    public void AClaimThatNeverCarriedTheVariation_isLeftAlone_andResetsTheBaseline()
    {
        var claims = new[] { new ClaimRespread.Claim("c8", IsDraft: false), new ClaimRespread.Claim("c9", IsDraft: false) };
        var prior = new[] { new ClaimRespread.PriorEntry("c9", "old-line", 100m, 1_749.78m) };

        var plan = ClaimRespread.Plan(claims, prior, RevisedLines(), AddedIds, oldTotal: 1_749.78m);

        Assert.DoesNotContain(plan, entry => entry.ValuationClaimId == "c8");
        Assert.Equal(1_749.78m, plan.Sum(entry => entry.PeriodIncrement));
    }

    [Fact]
    public void ALineTheClaimNeverCarried_isNotGivenMoneyRetrospectively()
    {
        // Two-line variation; Claim 9 only ever carried line A. Re-pricing B leaves Claim 9's entry
        // for A alone and does not invent one for B.
        var claims = new[] { new ClaimRespread.Claim("c9", IsDraft: false) };
        var prior = new[] { new ClaimRespread.PriorEntry("c9", "a", 100m, 100m) };

        var plan = ClaimRespread.Plan(claims, prior, new[] { ("a", 100m), ("b", 250m) }, new HashSet<string>(), oldTotal: 300m);

        var only = Assert.Single(plan);
        Assert.Equal("a", only.ValuationLineItemId);
        Assert.Equal(100m, only.CumulativeClaimed);
        Assert.Equal(100m, only.PercentComplete);
    }

    [Fact]
    public void AChangeOfValue_keepsTheSettledMoney_andDerivesThePercentage()
    {
        // The rule the settled side has always had, now with the percentage following the money:
        // £1,749.78 certified on a line now worth £2,000 is 87.489%.
        var claims = new[] { new ClaimRespread.Claim("c9", IsDraft: false) };
        var prior = new[] { new ClaimRespread.PriorEntry("c9", "old-line", 100m, 1_749.78m) };

        var plan = ClaimRespread.Plan(claims, prior, new[] { ("old-line", 2_000m) }, new HashSet<string>(), oldTotal: 1_749.78m);

        var only = Assert.Single(plan);
        Assert.Equal(1_749.78m, only.CumulativeClaimed);
        Assert.Equal(87.489m, only.PercentComplete);
    }
}
