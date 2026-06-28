using PdfSharp.Fonts;
// Alias because the unqualified name "Directory" otherwise binds to the sibling
// Jewel.JPMS.Api.Features.Directory namespace instead of System.IO.Directory.
using IODirectory = System.IO.Directory;

namespace Jewel.JPMS.Api.Features.Requests.Documents;

/// <summary>
/// Cross-platform font resolver for PDFsharp/MigraDoc. The renderer asks for a single logical family
/// ("JPMS Sans"); this resolver supplies a real TrueType sans-serif from the host so the same code
/// renders identically on a developer's Windows/macOS box and on the Linux Azure Functions host
/// (which ships almost no fonts by default).
///
/// Resolution order for the regular and bold faces:
///   1. An explicit file or directory from the RequestDocuments:FontPath app setting
///      (exposed to the process as the env var "RequestDocuments__FontPath").
///   2. A "fonts" folder next to the deployed application.
///   3. Well-known system font directories on Linux, Windows and macOS.
/// A list of common sans-serif file names is probed in each location (DejaVu, Liberation, Lato,
/// Arial, Helvetica, Verdana). If no bold file is found, bold is style-simulated from the regular
/// face; italic is always style-simulated. If not even a regular face can be found, the first render
/// throws a clear, actionable error rather than producing a corrupt PDF.
/// </summary>
public sealed class DocumentFontResolver : IFontResolver
{
    private const string RegularFace = "JPMS#R";
    private const string BoldFace = "JPMS#B";

    private static readonly string[] RegularCandidates =
    {
        "DejaVuSans.ttf", "LiberationSans-Regular.ttf", "Lato-Regular.ttf",
        "Arial.ttf", "arial.ttf", "Helvetica.ttf", "Verdana.ttf", "verdana.ttf",
        "FreeSans.ttf", "NotoSans-Regular.ttf", "OpenSans-Regular.ttf"
    };

    private static readonly string[] BoldCandidates =
    {
        "DejaVuSans-Bold.ttf", "LiberationSans-Bold.ttf", "Lato-Bold.ttf",
        "Arial-Bold.ttf", "arialbd.ttf", "Helvetica-Bold.ttf", "Verdana-Bold.ttf", "verdanab.ttf",
        "FreeSansBold.ttf", "NotoSans-Bold.ttf", "OpenSans-Bold.ttf"
    };

    private readonly object _gate = new();
    private byte[]? _regular;
    private byte[]? _bold;
    private bool _loaded;

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        EnsureLoaded();
        // Bold with a real bold file -> use it (italic still simulated). Bold with no bold file ->
        // simulate bold from the regular face. Anything else -> the regular face.
        if (isBold && _bold is not null)
            return new FontResolverInfo(BoldFace, false, isItalic);
        if (isBold)
            return new FontResolverInfo(RegularFace, true, isItalic);
        return new FontResolverInfo(RegularFace, false, isItalic);
    }

    public byte[]? GetFont(string faceName)
    {
        EnsureLoaded();
        return faceName == BoldFace ? _bold ?? _regular : _regular;
    }

    private void EnsureLoaded()
    {
        if (_loaded)
            return;

        lock (_gate)
        {
            if (_loaded)
                return;

            var directories = CandidateDirectories();
            _regular = FindFirst(directories, RegularCandidates);
            _bold = FindFirst(directories, BoldCandidates);

            if (_regular is null)
                throw new InvalidOperationException(
                    "No sans-serif TrueType font could be found for request-document rendering. " +
                    "Install a font package on the host (e.g. fonts-dejavu-core / fonts-liberation), " +
                    "drop a .ttf into a 'fonts' folder beside the application, or set the " +
                    "RequestDocuments:FontPath app setting to a .ttf file or a folder containing one.");

            _loaded = true;
        }
    }

    private static IReadOnlyList<string> CandidateDirectories()
    {
        var dirs = new List<string>();

        var configured = Environment.GetEnvironmentVariable("RequestDocuments__FontPath");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            // The override may point at a single .ttf or at a directory of fonts.
            if (File.Exists(configured))
                dirs.Add(Path.GetDirectoryName(configured)!);
            else if (IODirectory.Exists(configured))
                dirs.Add(configured);
        }

        dirs.Add(Path.Combine(AppContext.BaseDirectory, "fonts"));
        dirs.Add(AppContext.BaseDirectory);
        dirs.Add("/usr/share/fonts");
        dirs.Add("/usr/local/share/fonts");
        dirs.Add("/usr/share/fonts/truetype/lato");
        dirs.Add("/usr/share/fonts/truetype/dejavu");
        dirs.Add("/usr/share/fonts/truetype/liberation");

        var windir = Environment.GetEnvironmentVariable("WINDIR");
        if (!string.IsNullOrEmpty(windir))
            dirs.Add(Path.Combine(windir, "Fonts"));

        dirs.Add("/Library/Fonts");
        dirs.Add("/System/Library/Fonts");
        dirs.Add("/System/Library/Fonts/Supplemental");

        return dirs;
    }

    private static byte[]? FindFirst(IReadOnlyList<string> directories, string[] fileNames)
    {
        // First try exact file names in each directory (cheap, predictable).
        foreach (var dir in directories)
        {
            if (string.IsNullOrEmpty(dir) || !IODirectory.Exists(dir))
                continue;

            foreach (var name in fileNames)
            {
                var path = Path.Combine(dir, name);
                if (File.Exists(path))
                    return SafeRead(path);
            }
        }

        // Fall back to a recursive search for the same file names (handles nested font trees such as
        // /usr/share/fonts/truetype/<family>/...). Case-insensitive match on the file name.
        var wanted = new HashSet<string>(fileNames, StringComparer.OrdinalIgnoreCase);
        foreach (var dir in directories)
        {
            if (string.IsNullOrEmpty(dir) || !IODirectory.Exists(dir))
                continue;

            IEnumerable<string> files;
            try { files = IODirectory.EnumerateFiles(dir, "*.ttf", SearchOption.AllDirectories); }
            catch { continue; }

            foreach (var path in files)
            {
                if (wanted.Contains(Path.GetFileName(path)))
                {
                    var bytes = SafeRead(path);
                    if (bytes is not null)
                        return bytes;
                }
            }
        }

        return null;
    }

    private static byte[]? SafeRead(string path)
    {
        try { return File.ReadAllBytes(path); }
        catch { return null; }
    }
}
