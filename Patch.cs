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
                GameObject child = __instance.gameObject.transform.GetChild(0).gameObject;
                GameObject subChild = child.transform.GetChild(0).gameObject;
                GameObject.Destroy(subChild);
                //__instance.gameObject.GetComponent<ShamrockSpikedLogic>().Kill();

                GameObject newHelmet = ObjParser.LoadCustomProp("Helmet.obj");
                newHelmet.transform.parent = child.transform;
                newHelmet.transform.localPosition = new Vector3(0f, 0f, 0f);
                newHelmet.transform.localRotation = Quaternion.identity;

                string texturePath = Path.Combine(CanoneerRestoration.TexturesPath, "cannonbodytext.png");
                byte[] fileData = File.ReadAllBytes(texturePath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);
                newHelmet.GetComponent<MeshRenderer>().material.mainTexture = texture;
                return;
            }
        }
    }
}
