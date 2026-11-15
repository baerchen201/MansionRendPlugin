using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace MansionRend;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class MansionRend : BaseUnityPlugin
{
    internal const int LEVELID_REND = 6;
    internal const int DUNGEONFLOW_MANSION = 1;

    public static MansionRend Instance { get; private set; } = null!;
    internal static new ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony Harmony = null!;

    private ConfigEntry<uint> maxAttempts = null!;
    public uint MaxAttempts => maxAttempts.Value;

    private void Awake()
    {
        const string SECTION_GENERAL = "General";

        Logger = base.Logger;
        Instance = this;

        maxAttempts = Config.Bind(
            SECTION_GENERAL,
            nameof(MaxAttempts),
            (uint)short.MaxValue,
            "How many times the seed may be regenerated (prevents lockups)"
        );

        Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        Logger.LogDebug("Patching...");
        Harmony.PatchAll();
        Logger.LogDebug("Finished patching!");

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }
}
