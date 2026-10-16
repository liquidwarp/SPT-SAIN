using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Web;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace SAINServerMod;

public sealed record SAINServermodMetadata : IModMetadata, IModBlazorMetadata
{
    public string ModGuid { get; init; } = MyModInfo.MOD_GUID;
    public string Name { get; init; } = MyModInfo.MOD_NAME;
    public string Author { get; init; } = MyModInfo.MOD_AUTHOR;
    public List<string>? Contributors { get; init; } = [];
    public Version Version { get; init; } = new(MyModInfo.MOD_VERSION);
    public Range SptVersion { get; init; } = new(MyModInfo.SERVER_SPT_VERSION);
    public List<string>? Incompatibilities { get; init; } = [];
    public Dictionary<string, Range>? ModDependencies { get; init; } = [];
    public string? Url { get; init; } = "https://github.com/liquidwarp/SPT-SAIN";
    public bool? IsBundleMod { get; init; } = false;
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;
    public string? WWWRootUrl { get; init; } = "SAIN";
    public string? HomePage { get; init; } = "/sain/presets";
    public string? HomePageDescription { get; init; } = "Edit various SAIN settings from your browser.";
}
