using SPT.Common.Http;
using SPT.Common.Utils;

namespace SAIN.Preset.Server;

public static class ServerDataClient
{
    public static string Get(string key)
    {
        string body = Json.Serialize(new KeyBody { Name = key });
        string json = RequestHandler.PutJson("/sain/data/get", body);
        return json == "null" ? null : json;
    }

    public static string GetBotTypes()
    {
        return RequestHandler.GetJson("/sain/bottypes/list");
    }

    public static string GetBotTypeExclusions()
    {
        return RequestHandler.GetJson("/sain/bottypes/exclusions");
    }

    private sealed class KeyBody
    {
        public string Name;
    }
}
