using HarmonyLib;
using SFS.World;
using SFS.World.Maps;

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace WorldBuild.Patches
{
    public static class Patches
    {
        /// <summary>
        /// Skip camera movement when dragging a part in-world.
        /// </summary>
        [HarmonyPatch(typeof(PlayerController), "OnDrag")]
        static class PlayerController_OnDrag
        {
            static bool Prefix()
            {
                return !Manager.main.draggingPart;
            }
        }

        /// <summary>
        /// Skip interactions with parts on a rocket when building in-world.
        /// </summary>
        [HarmonyPatch(typeof(Rocket), nameof(Rocket.OnInputEnd_AsPlayer))]
        static class Rocket_OnInputEnd_AsPlayer
        {
            static bool Prefix()
            {
                return !Manager.main.worldBuildActive;
            }
        }

        /// <summary>
        /// Close world build when map is opened.
        /// </summary>
        [HarmonyPatch(typeof(MapManager), nameof(MapManager.ToggleMap))]
        static class MapManager_ToggleMap
        {
            static void Postfix()
            {
                Manager.main.ExitBuild();
            }
        }

        /// <summary>
        /// Close world build when world is unloaded.
        /// </summary>
        [HarmonyPatch(typeof(GameManager), "ClearWorld")]
        static class GameManager_ClearWorld
        {
            static void Prefix()
            {
                Manager.main.ExitBuild();
            }
        }
    }
}