using HarmonyLib;
using UnityEngine;
using System.IO;

namespace CanoneerRestoration;

public class Patch
{
    [HarmonyPatch(typeof(ShamrockSpikedLogic), "Start")]
    public class ShamrockSpikedLogicStart
    {
        public static bool Prefix(ShamrockSpikedLogic __instance)
        {
            var random = new System.Random();
            if (CanoneerRestoration.CanoneerSpawnChance.Value >= random.Next(0, 100))
            {
                GameObject spawnController;
                bool trackPlayer;
                bool detected;
                bool spawning;
                
                GameObject child = __instance.gameObject.transform.GetChild(0).gameObject;
                GameObject subChild = child.transform.GetChild(0).gameObject;
                GameObject.Destroy(subChild);

                ShamrockSpikedLogic originalAi = __instance.gameObject.GetComponent<ShamrockSpikedLogic>();

                spawnController = originalAi.spawnController;
                trackPlayer = originalAi.trackPlayer;
                detected = originalAi.detected;
                spawning = originalAi.spawning;
                int originalHealth = originalAi.health; // Cache health before destroying component

                GameObject newHelmet = ObjParser.LoadCustomProp("Helmet.obj");
                newHelmet.transform.parent = child.transform;
                newHelmet.transform.localPosition = Vector3.zero;
                newHelmet.transform.localRotation = Quaternion.identity;

                string texturePath = Path.Combine(CanoneerRestoration.TexturesPath, "cannonbodytext.png");
                byte[] fileData = File.ReadAllBytes(texturePath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);
                newHelmet.GetComponent<MeshRenderer>().material.mainTexture = texture;

                ShamrockCanoneerLogic canoneerLogic = __instance.gameObject.AddComponent<ShamrockCanoneerLogic>();
                canoneerLogic.spawnControllerObject = spawnController;
                canoneerLogic.trackPlayer = trackPlayer;
                canoneerLogic.detected = detected;
                canoneerLogic.spawning = spawning;
                
                if (spawnController != null)
                {
                    ShamrockSpawnController spawnControllerComponent = spawnController.GetComponent<ShamrockSpawnController>();
                    if (spawnControllerComponent != null)
                    {
                        int healthDiff = canoneerLogic.health/* - originalHealth*/;
                        spawnControllerComponent.maxCombinedHealth += healthDiff;
                        spawnControllerComponent.combinedHealth += healthDiff;
                    }
                }

                // Destroy the old component now that we've pulled everything we need
                GameObject.Destroy(originalAi);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CannonballLogic), "Kill")]
    public class CannonballLogicKill
    {
        public static bool Prefix(CannonballLogic __instance)
        {
            // Prevent NullReferenceException by using our custom shrapnel if the base game's prefab is null
            // This is a defensive fix for when Kill() tries to instantiate a null prefab
            if (__instance.shrapnel == null)
            {
                __instance.shrapnel = CannonballFactory.CreateShrapnelPrefab();
            }
            return true;
        }
    }

    // Test patch
    [HarmonyPatch(typeof(ShamrockSpawnController), "Increment")]
    public class TestSpawnController
    {
        public static void Postfix(ShamrockSpawnController __instance)
        {
            CanoneerRestoration.Logger.LogInfo($"Spawn controller incremented! Current count: {__instance.kills}");
        }
    }
    [HarmonyPatch(typeof(ShamrockSpawnController), "LostHealth")]
    public class TestSpawnControllerLostHealth
    {
        public static void Postfix(ShamrockSpawnController __instance)
        {
            CanoneerRestoration.Logger.LogInfo($"Spawn controller lost health! Current health: {__instance.combinedHealth} / {__instance.maxCombinedHealth}");
        }
    }
}
