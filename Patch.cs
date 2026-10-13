using HarmonyLib;
using UnityEngine;
using System.IO;

namespace CanoneerRestoration;

public class Patch
{
    [HarmonyPatch(typeof(ShamrockSpikedLogic), "Start")]
    public class ShamrockSpikedLogicStart
    {
        public static void Prefix(ShamrockSpikedLogic __instance)
        {
            var random = new System.Random();
            if (CanoneerRestoration.CanoneerSpawnChance.Value >= random.Next(0, 100))
            {
                GameObject spawnController;
                bool trackPlayer;
                bool detected;

                GameObject child = __instance.gameObject.transform.GetChild(0).gameObject;
                GameObject subChild = child.transform.GetChild(0).gameObject;
                GameObject.Destroy(subChild);

                spawnController = __instance.gameObject.GetComponent<ShamrockSpikedLogic>().spawnController;

                GameObject.Destroy(__instance.gameObject.GetComponent<ShamrockSpikedLogic>());
                trackPlayer = __instance.gameObject.GetComponent<ShamrockSpikedLogic>().trackPlayer;
                detected = __instance.gameObject.GetComponent<ShamrockSpikedLogic>().detected;

                GameObject newHelmet = ObjParser.LoadCustomProp("Helmet.obj");
                newHelmet.transform.parent = child.transform;
                newHelmet.transform.localPosition = new Vector3(0f, 0f, 0f);
                newHelmet.transform.localRotation = Quaternion.identity;

                string texturePath = Path.Combine(CanoneerRestoration.TexturesPath, "cannonbodytext.png");
                byte[] fileData = File.ReadAllBytes(texturePath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);
                newHelmet.GetComponent<MeshRenderer>().material.mainTexture = texture;

                ShamrockCanoneerLogic canoneerLogic = __instance.gameObject.AddComponent<ShamrockCanoneerLogic>();
                canoneerLogic.spawnController = spawnController;
                canoneerLogic.trackPlayer = trackPlayer;
                canoneerLogic.detected = detected;
                return;
            }
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
}
