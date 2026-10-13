using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.IO;

namespace CanoneerRestoration;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class CanoneerRestoration : BaseUnityPlugin
{
    public static new ManualLogSource Logger;

    public static ConfigEntry<int> CanoneerSpawnChance;

    public static string BasePath { get; private set; } = string.Empty;
    public static string ModelsPath { get; private set; } = string.Empty;
    public static string TexturesPath { get; private set; } = string.Empty;
    public static string AudioPath { get; private set; } = string.Empty;

    private void Awake()
    {
        // Initialize config
        CanoneerSpawnChance = Config.Bind("General", "Canoneer spawn chance", 33, 
            new ConfigDescription("Chance for Canoneer to spawn instead of regular ShamrockSpiked (default: 33)", new AcceptableValueRange<int>(0, 100)));


        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        
        // Set paths
        BasePath = Path.Combine(Paths.PluginPath, MyPluginInfo.PLUGIN_GUID);
        BasePath = Path.Combine(BasePath, "Resources");
        ModelsPath = Path.Combine(BasePath, "models");
        TexturesPath = Path.Combine(BasePath, "textures");
        AudioPath = Path.Combine(BasePath, "audio");
        
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
    }
}
