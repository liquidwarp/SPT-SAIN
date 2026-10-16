using System;

namespace SAIN.Preset.Shared;

public static class ForkVersionInfo
{
    public const string Marker = MyModInfo.FORK_MARKER;

    private const string Suffix = "-" + Marker + "-";

    public static bool TryGetRevision(string version, out int revision)
    {
        revision = 0;
        int start = version.IndexOf(Suffix, StringComparison.Ordinal);
        if (start < 0)
        {
            return false;
        }

        start += Suffix.Length;
        int end = start;
        while (end < version.Length && char.IsDigit(version[end]))
        {
            end++;
        }
        return end > start && int.TryParse(version.Substring(start, end - start), out revision);
    }
}
