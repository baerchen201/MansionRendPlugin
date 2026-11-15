using System;
using System.Linq;
using HarmonyLib;

namespace MansionRend.Patches;

[HarmonyPatch(typeof(RoundManager), nameof(RoundManager.LoadNewLevelWait))]
internal static class RoundManager_LoadNewLevelWait
{
    private static void Prefix(ref RoundManager __instance, ref int randomSeed)
    {
        try
        {
            if (__instance.playersManager.isChallengeFile)
            {
                MansionRend.Logger.LogDebug("Mod disabled on challenge moons");
                return;
            }

            var level = __instance.currentLevel;
            if (level.levelID != MansionRend.LEVELID_REND)
            {
                MansionRend.Logger.LogDebug(
                    $"{nameof(level.levelID)} {level.levelID} is not rend ({MansionRend.LEVELID_REND})"
                );
                return;
            }

            if (
                !level.dungeonFlowTypes.Any(i =>
                    i.id == MansionRend.DUNGEONFLOW_MANSION && i.rarity > 0
                )
            )
            {
                MansionRend.Logger.LogWarning("Mansion interior can not generate");
                return;
            }

            MansionRend.Logger.LogDebug(
                $"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} is protecting your rend experience :)"
            );

            // this basically brute forces up to MaxAttempts seeds until it finds the mansion interior, otherwise if you just change the interior id clients would get desynced

            var weights = __instance.currentLevel.dungeonFlowTypes.Select(i => i.rarity).ToArray();
            var dungeonFlowTypes = __instance.currentLevel.dungeonFlowTypes;

            for (uint i = MansionRend.Instance.MaxAttempts; i > 0; i--)
            {
                var interior = __instance.GetRandomWeightedIndex(weights, new Random(randomSeed));
                try
                {
                    if (dungeonFlowTypes[interior].id == MansionRend.DUNGEONFLOW_MANSION)
                    {
#if DEBUG
                        MansionRend.Logger.LogDebug(
                            $"Seed {randomSeed, 8} generated mansion interior, resuming level generation"
                        );
#endif
                        return;
                    }
#if DEBUG
                    try
                    {
                        MansionRend.Logger.LogDebug(
                            $"Seed {randomSeed, 8} generated {__instance.dungeonFlowTypes[__instance.currentLevel.dungeonFlowTypes[interior].id].dungeonFlow.name} interior, regenerating seed..."
                        );
                    }
                    catch
                    {
                        MansionRend.Logger.LogDebug(
                            $"Seed {randomSeed, 8} generated interior #{__instance.currentLevel.dungeonFlowTypes[interior].id}, regenerating seed..."
                        );
                    }
#endif
                }
                catch (IndexOutOfRangeException)
                {
                    MansionRend.Logger.LogWarning(
                        $"Seed {randomSeed, 7} generated unknown interior #{interior}, regenerating seed..."
                    );
                }

                __instance.playersManager.ChooseNewRandomMapSeed();
                randomSeed = __instance.playersManager.randomMapSeed;
            }
            MansionRend.Logger.LogWarning($"{nameof(MansionRend.MaxAttempts)} exceeded");
        }
        catch (Exception e)
        {
            MansionRend.Logger.LogError($"An error occurred trying to adjust interior: {e}");
        }
    }
}
