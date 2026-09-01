namespace Jewel.JPMS;

/// <summary>How a file size reads to a person: "3.2 MB", "412 KB", "87 B".</summary>
public static class FileSizeFormat
{
    public static string FormatSize(long bytes)
    {
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:0.#} MB";
        if (bytes >= 1024) return $"{bytes / 1024.0:0.#} KB";
        return $"{bytes} B";
    }

    public static string FormatSize(long? bytes) =>
        bytes is { } size ? FormatSize(size) : "Unknown size";
}
