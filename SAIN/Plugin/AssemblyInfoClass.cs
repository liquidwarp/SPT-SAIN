using System.Reflection;

[assembly: AssemblyTitle(SAIN.AssemblyInfoClass.Description)]
[assembly: AssemblyDescription(SAIN.AssemblyInfoClass.Description)]
[assembly: AssemblyCopyright(SAIN.AssemblyInfoClass.Copyright)]
[assembly: AssemblyFileVersion(SAIN.MyModInfo.MOD_VERSION)]

namespace SAIN;

public static class AssemblyInfoClass
{
    public const string Title = SAINName;
    public const string Description = "Full Revamp of Escape from Tarkov's AI System.";
    public const string Configuration = SPTVersion;
    public const string Company = "";
    public const string Product = SAINName;
    public const string Copyright = "Copyright © 2026 Solarint";
    public const string Trademark = "";
    public const string Culture = "";

    public const int TarkovVersion = 40743;

    public const string EscapeFromTarkov = "EscapeFromTarkov.exe";

    public const string SAINGUID = MyModInfo.MOD_GUID;
    public const string SAINName = MyModInfo.MOD_NAME;

    public const string SPTVersion = MyModInfo.CLIENT_SPT_VERSION;

    public const string SPTGUID = "com.SPT.core";
    public const string FikaGUID = "com.fika.core";
    public const string FikaHeadlessGUID = "com.fika.headless";

    public const string BigBrainGUID = "xyz.drakia.bigbrain";
    public const string BigBrainVersion = "1.5.0";
}
