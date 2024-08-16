using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UITools;
using SFS.IO;
using ModLoader;
using ModLoader.Helpers;
using WorldBuild.GUI;
using WorldBuild.Settings;

namespace WorldBuild
{
    public class Main : Mod
    {
        public static Main main;
        public override string ModNameID => "worldbuild";
        public override string DisplayName => "World Build";
        public override string Author => "Astro The Rabbit";
        public override string MinimumGameVersionNecessary => "1.5.10.2";
        public override string ModVersion => "1.0";
        public override string Description => "A test mod for adding parts to rockets in-flight.";

        public override Dictionary<string, string> Dependencies { get; } = new Dictionary<string, string> { { "UITools", "1.1.5" } };
        // public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>() { { "https://github.com/AstroTheRabbit/World-Build-SFS/releases/latest/download/WorldBuild.dll", new FolderPath(ModFolder).ExtendToFile("WorldBuild.dll") } };

        public override void Early_Load()
        {
            new Harmony(ModNameID).PatchAll();
            main = this;
        }

        public override void Load()
        {
            Keybinds.Init();

            GameObject go = new GameObject("World Build: Manager");
            Object.DontDestroyOnLoad(go);
            Manager.main = go.AddComponent<Manager>();

            SceneHelper.OnWorldSceneLoaded += Manager.main.AddInputs;
            SceneHelper.OnWorldSceneLoaded += PartPickerUI.DestroyCreatedParts;
            SceneHelper.OnWorldSceneUnloaded += () => Manager.main.worldBuildActive = false;
        }
    }
}
