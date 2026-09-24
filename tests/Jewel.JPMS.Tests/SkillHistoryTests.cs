using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Ai.Skills;
using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// Every past version of a stored skill and its references can be audited and restored (2026-09-24,
// Nigel: "we may need to audit stuff", then "ensure he can revert back to a skill easily too"). A
// save keeps the version it replaces whole; a restore is a new version, so nothing is ever lost.
public sealed partial class SkillHistoryTests
{
    private const string Key = "commercial-doctrine";
    private const string Nigel = "nigel@jewelbb.co.uk";
    private const string James = "james@jewelbb.co.uk";

    private static JpmsContext NewContext() =>
        new(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"skill-history-{Guid.NewGuid():N}").Options);

    private static Task SaveAsync(JpmsContext context, string body, string by) =>
        new SaveAiSkillHandler(context).HandleAsync(
            new SaveAiSkill(Key, "shared", "Commercial doctrine", $"When {body}", body, true, true, by), default);

    private static Task SaveReferenceAsync(JpmsContext context, string body, string by) =>
        new SaveAiSkillReferenceHandler(context).HandleAsync(
            new SaveAiSkillReference(Key, "clause-map", "Clause map", "Clauses", body, by), default);

    private static Task<SkillHistory?> HistoryAsync(JpmsContext context) =>
        new GetAiSkillHistoryHandler(context).HandleAsync(new GetAiSkillHistory(Key), default);

    private static RestoreAiSkillVersionHandler Restorer(JpmsContext context) =>
        new(context, new GetAiSkillHistoryHandler(context), new SaveAiSkillHandler(context), new SaveAiSkillReferenceHandler(context));

    [Fact]
    public async Task EverySave_keepsTheVersionItReplaces_withItsWriterAndItsTimes()
    {
        await using var context = NewContext();
        await SaveAsync(context, "first", Nigel);
        await SaveAsync(context, "second", James);
        await SaveAsync(context, "third", Nigel);

        var history = (await HistoryAsync(context))!;

        Assert.Equal(new[] { 3, 2, 1 }, history.Versions.Select(version => version.Version));
        Assert.Equal(new[] { "third", "second", "first" }, history.Versions.Select(version => version.Body));
        Assert.Equal(new[] { Nigel, James, Nigel }, history.Versions.Select(version => version.WrittenByEmail));
        Assert.True(history.Versions[0].IsCurrent);
        Assert.All(history.Versions, version => Assert.NotNull(version.WrittenAt));
        Assert.Equal(history.Versions[2].ReplacedAt, history.Versions[1].WrittenAt);
    }

    [Fact]
    public async Task RestoringAVersion_savesItAsANewVersion_andKeepsEveryOther()
    {
        await using var context = NewContext();
        await SaveAsync(context, "first", Nigel);
        await SaveAsync(context, "second", Nigel);

        await Restorer(context).HandleAsync(new RestoreAiSkillVersion(Key, null, 1, James), default);

        var history = (await HistoryAsync(context))!;
        Assert.Equal(new[] { "first", "second", "first" }, history.Versions.Select(version => version.Body));
        Assert.Equal(James, history.Versions[0].WrittenByEmail);
        Assert.True(history.Versions[0].Pinned);
    }

    [Fact]
    public async Task RestoringTheVersionInForce_isRefused()
    {
        await using var context = NewContext();
        await SaveAsync(context, "first", Nigel);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Restorer(context).HandleAsync(new RestoreAiSkillVersion(Key, null, 1, James), default));
    }

    [Fact]
    public async Task AReferenceDocument_isVersionedAndRestoredTheSameWay()
    {
        await using var context = NewContext();
        await SaveAsync(context, "skill", Nigel);
        await SaveReferenceAsync(context, "clauses v1", Nigel);
        await SaveReferenceAsync(context, "clauses v2", James);

        await Restorer(context).HandleAsync(new RestoreAiSkillVersion(Key, "clause-map", 1, Nigel), default);

        var reference = Assert.Single((await HistoryAsync(context))!.References);
        Assert.Equal(new[] { "clauses v1", "clauses v2", "clauses v1" }, reference.Versions.Select(version => version.Body));
        Assert.Equal(new[] { 3, 2, 1 }, reference.Versions.Select(version => version.Version));
    }

    [Fact]
    public void TheHistory_andTheRestore_reachTheConnector_forTheSkillGateOnly()
    {
        var director = AiToolCatalogue.ForConnector(new SignedInUser("md@jewelbb.co.uk", "MD", new[] { Role.ManagingDirector }));
        Assert.Equal(AiToolKind.Read, Assert.Single(director, tool => tool.Name == "list_skill_history").Kind);
        Assert.Equal(AiToolKind.Write, Assert.Single(director, tool => tool.Name == "restore_skill_version").Kind);

        var surveyor = AiToolCatalogue.ForConnector(new SignedInUser("qs@jewelbb.co.uk", "QS", new[] { Role.QuantitySurveyor }))
            .Select(tool => tool.Name).ToList();
        Assert.DoesNotContain("list_skill_history", surveyor);
        Assert.DoesNotContain("restore_skill_version", surveyor);
    }
}
