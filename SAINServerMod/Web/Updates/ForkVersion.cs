using System.Diagnostics.CodeAnalysis;
using SAIN.Preset.Shared;
using Version = SemanticVersioning.Version;

namespace SAINServerMod.Web.Updates;

public sealed record ForkVersion(Version Base, int Revision) : IComparable<ForkVersion>
{
    public static bool TryParse(string text, [NotNullWhen(true)] out ForkVersion? version)
    {
        version = null;
        if (!Version.TryParse(text.TrimStart('v', 'V'), out Version semver))
        {
            return false;
        }

        int revision = 0;
        if (semver.IsPreRelease && !ForkVersionInfo.TryGetRevision($"-{semver.PreRelease}", out revision))
        {
            return false;
        }

        version = new ForkVersion(semver.BaseVersion(), revision);
        return true;
    }

    public int CompareTo(ForkVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        int result = Base.CompareTo(other.Base);
        return result != 0 ? result : Revision.CompareTo(other.Revision);
    }

    public static bool operator >(ForkVersion a, ForkVersion b)
    {
        return a.CompareTo(b) > 0;
    }

    public static bool operator <(ForkVersion a, ForkVersion b)
    {
        return a.CompareTo(b) < 0;
    }

    public override string ToString()
    {
        return Revision > 0 ? $"{Base}-{ForkVersionInfo.Marker}-{Revision}" : Base.ToString();
    }
}
