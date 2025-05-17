using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace MansionRend;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class MansionRend : BaseUnityPlugin
{
    internal const int REND = 6;
    internal const int MANSION = 1;

    internal static new ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    private void Awake()
    {
        Logger = base.Logger;

        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);
        Logger.LogDebug("Patching...");
        Harmony.PatchAll();
        Logger.LogDebug("Finished patching!");

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.LoadNewLevelWait))]
    public class LoadNewLevelWaitPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Prefix(ref RoundManager __instance, ref int randomSeed)
        {
            Logger.LogDebug(
                $">> LoadNewLevelWait: {__instance.currentLevel.PlanetName} (seed:{randomSeed}, id:{__instance.currentLevel.levelID}) {__instance.currentLevel.dungeonFlowTypes.Length} interiors found:"
            );

            foreach (IntWithRarity i in __instance.currentLevel.dungeonFlowTypes)
                Logger.LogDebug($"   {i.id} - {i.rarity}");

            if (__instance.currentLevel.levelID != REND)
                return;

            Logger.LogDebug("Finding seed with mansion generation...");
            try
            {
                while (!IsGeneration(ref __instance, randomSeed, MANSION))
                {
                    Logger.LogDebug(
                        $"Seed {randomSeed} does not have mansion generation, trying next seed"
                    );
                    StartOfRound.Instance.ChooseNewRandomMapSeed();
                    randomSeed = StartOfRound.Instance.randomMapSeed;
                }
                Logger.LogDebug($"Seed {randomSeed} has mansion generation, proceeding");
            }
            catch (Exception e)
            {
                Logger.LogError($"An unexpected error occurred: {e.GetType().Name} {e.StackTrace}");
            }

            if (IsGeneration(ref __instance, randomSeed, MANSION))
                Logger.LogDebug($"<< LoadNewLevelWait: seed {randomSeed} has mansion generation");
            else
                Logger.LogError(
                    $"<< LoadNewLevelWait: seed {randomSeed} does not have mansion generation"
                );
        }

        internal static bool IsGeneration(
            ref RoundManager __instance,
            int randomSeed,
            int generationId
        )
        {
            if (
                __instance.currentLevel.dungeonFlowTypes == null
                || __instance.currentLevel.dungeonFlowTypes.Length == 0
            )
                return false;

            List<int> list = [];
            list.AddRange(__instance.currentLevel.dungeonFlowTypes.Select(i => i.rarity));

            int randomWeightedIndex = __instance.GetRandomWeightedIndex(
                list.ToArray(),
                new Random(randomSeed)
            );
            return __instance.currentLevel.dungeonFlowTypes[randomWeightedIndex].id == generationId;
        }
    }
}
