using Xunit;

namespace Jewel.JPMS.Tests;

// Every email the portal sends leaves an audit row, because every one of them is staged and sent
// in the same place. Four doors did it themselves until 2026-09-18 — the sales reply (which sent
// and recorded nothing anywhere), triage's "Reply in thread" and the purchase order's covering
// reply (drafts, one audited by hand and one not at all), and a background worker that rendered a
// request document and staged it (a whole second Graph client, since deleted). They drifted
// because nothing stopped them drifting; this is what stops them.
public sealed class OutboundSequenceTests
{
    private static readonly string[] StagesOrSends =
        { "CreateDraftAsync(", "CreateReplyDraftAsync(", "SendDraftAsync(" };

    /// <summary>The files allowed to call the mailbox's draft and send methods. The dispatcher is
    /// the one way a RECORD's email leaves. Triage compose is the sequence the dispatcher was
    /// taken from and cannot use it: it rewrites the draft's envelope between staging and sending,
    /// which is a step the dispatcher deliberately does not have.</summary>
    private static readonly string[] Owners =
    {
        "OutboundEmailDispatcher.cs",
        "OutboundEmailDispatcher.Outcomes.cs",
        "SendMailboxEmailHandler.cs",
        "SendMailboxEmailHandler.Draft.cs",
    };

    [Fact]
    public void NothingOutsideTheDispatcher_stagesOrSendsAnEmailItself()
    {
        var sources = PortalSources().ToList();
        Assert.NotEmpty(sources);

        var offenders = sources
            .Where(file => !Owners.Contains(file.Name))
            .Where(file => !IsTheGraphClientItself(file))
            .Where(file => StagesOrSends.Any(call => File.ReadAllText(file.FullName).Contains(call)))
            .Select(file => file.Name)
            .OrderBy(name => name)
            .ToList();

        Assert.True(offenders.Count == 0,
            "These talk to the mailbox's draft/send methods directly instead of asking "
            + "OutboundEmailDispatcher, so whatever they send can go unrecorded:\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void EveryDoorThatDispatches_saysWhereItsEmailBelongs()
    {
        // A dispatch carries a filing — the pathway and the record the audit row is found by — so
        // the two always appear together. A door that starts dispatching without one would be
        // writing rows nobody can search, and this is what would notice.
        var dispatching = PortalSources()
            .Select(file => new { file.Name, Text = File.ReadAllText(file.FullName) })
            .Where(source => source.Text.Contains("dispatcher.Dispatch"))
            .ToList();

        Assert.NotEmpty(dispatching);
        var unfiled = dispatching
            .Where(source => !source.Text.Contains("new OutboundEmailFiling("))
            .Select(source => source.Name)
            .ToList();

        Assert.True(unfiled.Count == 0,
            "These dispatch an email without saying where it belongs:\n" + string.Join("\n", unfiled));
    }

    private static bool IsTheGraphClientItself(FileInfo file) =>
        file.Directory?.Name == "Graph" || file.Name.StartsWith("RecordingMailbox", StringComparison.Ordinal);

    private static IEnumerable<FileInfo> PortalSources()
    {
        var root = RepoRoot();
        foreach (var area in new[] { "api", "worker", "jpms" })
        {
            var folder = new DirectoryInfo(Path.Combine(root.FullName, area));
            if (!folder.Exists) continue;
            foreach (var file in folder.GetFiles("*.cs", SearchOption.AllDirectories))
                if (!IsBuildOutput(file)) yield return file;
        }
    }

    private static bool IsBuildOutput(FileInfo file) =>
        file.FullName.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || file.FullName.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    private static DirectoryInfo RepoRoot()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (Directory.Exists(Path.Combine(folder.FullName, "api"))
                && Directory.Exists(Path.Combine(folder.FullName, "contracts"))) return folder;
        }
        throw new DirectoryNotFoundException($"The repository root was not found above {AppContext.BaseDirectory}.");
    }
}
