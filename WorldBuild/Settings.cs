using UnityEngine;
using ModLoader;
using ModLoader.Helpers;
using static SFS.Input.KeybindingsPC;

namespace WorldBuild.Settings
{
    // public class Config : ModSettings<ConfigData>
    // {
    //     protected override FilePath SettingsFile => new FolderPath(Main.main.ModFolder).ExtendToFile("config.txt");

    //     protected override void RegisterOnVariableChange(Action onChange)
    //     {
    //         Application.quitting += onChange;
    //     }
    // }
    // public class ConfigData
    // {

    // }

    public class Keybinds : ModKeybindings
    {
        public static Keybinds main;

        public Key toggleWorldBuild = Key.Ctrl_(KeyCode.B);

        public static void Init()
        {
            main = SetupKeybindings<Keybinds>(Main.main);
            SceneHelper.OnWorldSceneLoaded += AssignFunctions;
        }

        public override void CreateUI()
        {
            Keybinds defaults = new Keybinds();

			CreateUI_Text("Smart SAS");
            CreateUI_Keybinding(toggleWorldBuild, defaults.toggleWorldBuild, "Toggle World Build");
        }

        public static void AssignFunctions()
        {
            AddOnKeyDown_World(main.toggleWorldBuild, Manager.ToggleBuild);
        }
    }
}