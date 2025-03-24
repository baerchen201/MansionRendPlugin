using System;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace MansionRend;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class MansionRend : BaseUnityPlugin
{
    public static MansionRend Instance { get; private set; } = null!;
    internal static new ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        Patch();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GenerateNewFloor))]
    public class GenerateNewFloorPatch
    {
        private static void Prefix(ref RoundManager __instance)
        {
            Logger.LogDebug(
                $">> GenerateNewFloor: {__instance.currentLevel.PlanetName} (id:{__instance.currentLevel.levelID}) {__instance.currentLevel.dungeonFlowTypes.Length} interiors found:"
            );
            foreach (IntWithRarity i in __instance.currentLevel.dungeonFlowTypes)
                Logger.LogDebug($"   {i.id} - {i.rarity}");

            if (__instance.currentLevel.levelID != 6) // Rend
                return;

            Logger.LogDebug("Overwriting indoor generation chances...");
            try
            {
                LevelAmbienceLibrary overrideLevelAmbience = (
                    __instance.currentLevel.dungeonFlowTypes.Where((i) => i.id == 1)
                )
                    .First()
                    .overrideLevelAmbience;
                __instance.currentLevel.dungeonFlowTypes =
                [
                    new IntWithRarity()
                    {
                        id = 1,
                        rarity = 300,
                        overrideLevelAmbience = overrideLevelAmbience,
                    },
                ];
            }
            catch (Exception e)
            {
                Logger.LogError($"An unexpected error occurred: {e.GetType().Name} {e.StackTrace}");
                Logger.LogWarning(
                    "Skipping overrideLevelAmbience, this may cause errors in other parts of the game code."
                );
                __instance.currentLevel.dungeonFlowTypes =
                [
                    new IntWithRarity() { id = 1, rarity = 300 },
                ];
            }

            Logger.LogDebug(
                $"<< GenerateNewFloor: {__instance.currentLevel.PlanetName} (id:{__instance.currentLevel.levelID}) {__instance.currentLevel.dungeonFlowTypes.Length} interiors found:"
            );
            foreach (IntWithRarity i in __instance.currentLevel.dungeonFlowTypes)
                Logger.LogDebug($"   {i.id} - {i.rarity}");
        }
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        Logger.LogDebug("Finished patching!");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }
}
