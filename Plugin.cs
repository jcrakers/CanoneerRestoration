using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Linq;

namespace CanoneerRestoration;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class CanoneerRestoration : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    public static ConfigEntry<int> CanoneerSpawnChance;

    private void Awake()
    {
        // Initialize config
        CanoneerSpawnChance = Config.Bind("General", "Canoneer spawn chance", 33, 
            new ConfigDescription("Chance for Canoneer to spawn instead of regular ShamrockSpiked (default: 33)", new AcceptableValueRange<int>(0, 100)));


        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
    }

    private void Update()
    {
        // Press F9 in-game to trigger the dump
        if (Input.GetKeyDown(KeyCode.F9))
        {
            DumpUnusedAssetReferences("SignManAttack");
        }
    }

    private void DumpUnusedAssetReferences(string searchTerm)
    {
        Logger.LogInfo($"--- STARTING MEMORY DUMP FOR: '{searchTerm}' ---");

        // 1. Search Meshes
        Mesh[] meshes = Resources.FindObjectsOfTypeAll<Mesh>();
        var matchingMeshes = meshes.Where(m => m.name.IndexOf(searchTerm, System.StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        Logger.LogInfo($"Found {matchingMeshes.Count} matching Meshes:");
        foreach (var mesh in matchingMeshes)
        {
            Logger.LogInfo($"  [Mesh] Name: {mesh.name} | Vertices: {mesh.vertexCount}");
        }

        // 2. Search Materials
        Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
        var matchingMaterials = materials.Where(m => m.name.IndexOf(searchTerm, System.StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        Logger.LogInfo($"Found {matchingMaterials.Count} matching Materials:");
        foreach (var mat in matchingMaterials)
        {
            Logger.LogInfo($"  [Material] Name: {mat.name} | Shader: {mat.shader?.name}");
        }

        // 3. Search Textures
        Texture2D[] textures = Resources.FindObjectsOfTypeAll<Texture2D>();
        var matchingTextures = textures.Where(t => t.name.IndexOf(searchTerm, System.StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        Logger.LogInfo($"Found {matchingTextures.Count} matching Textures:");
        foreach (var tex in matchingTextures)
        {
            Logger.LogInfo($"  [Texture2D] Name: {tex.name} | Size: {tex.width}x{tex.height}");
        }

        // 4. Search Any Generic Inactive GameObjects
        GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        var matchingGOs = gameObjects.Where(g => g.name.IndexOf(searchTerm, System.StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        Logger.LogInfo($"Found {matchingGOs.Count} matching GameObjects in memory:");
        foreach (var go in matchingGOs)
        {
            Logger.LogInfo($"  [GameObject] Name: {go.name} | Active In Hierarchy: {go.activeInHierarchy}");
        }

        Logger.LogInfo($"--- END MEMORY DUMP ---");
    }
}
