using Xunit;

namespace Jewel.JPMS.Tests;

// The skills under docs/ai/skills are the working doctrine the team teaches the connector, and
// they name tools and actions on nearly every line. Renames on 2026-09-17 turned two of those
// names into promises the portal could not keep — jpms-tender-award sent the model to
// prepare_work_order_email_draft and prepare_bid_package_invite_draft, both deleted — and the
// build said nothing, because EveryToolOrActionACatalogueTextNames_exists reads the catalogue's
// own text and never these files. This is that guard, pointed at the doctrine.
//
// It reads the files rather than the portal's stored copies, which are a database the test cannot
// see. The two must be kept in step by hand (save_skill after a deploy), so a name that is wrong
// here is wrong in both.
public sealed class SkillFileNamesTests
{
    [Fact]
    public void EveryToolOrActionASkillNames_exists()
    {
        var skills = SkillsFolder().GetFiles("*.md", SearchOption.AllDirectories);
        Assert.NotEmpty(skills);

        var dangling = skills
            .SelectMany(skill => ConnectorNameReferences.DanglingIn(skill.Name, File.ReadAllText(skill.FullName)))
            .Distinct()
            .ToList();

        Assert.True(dangling.Count == 0,
            "A stored skill names tools that do not exist — fix the file AND re-save it with "
            + "save_skill, because the connector loads the database copy:\n" + string.Join("\n", dangling));
    }

    private static DirectoryInfo SkillsFolder()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            var skills = new DirectoryInfo(Path.Combine(folder.FullName, "docs", "ai", "skills"));
            if (skills.Exists) return skills;
        }
        throw new DirectoryNotFoundException($"docs/ai/skills was not found above {AppContext.BaseDirectory}.");
    }
}
