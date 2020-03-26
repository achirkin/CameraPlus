using HarmonyLib;
using UnityEngine;

namespace CameraPlus
{
    [HarmonyPatch(typeof(ObstacleController))]
    [HarmonyPatch("Init", MethodType.Normal)]
    public class TransparentWallsPatch
    {
        public static int WallLayerMask = 26;//Moved to layer 26 since FixMRAlpha uses layer 25 in 1.8.0
        private static void Postfix(ref ObstacleController __instance)
        {
            Camera.main.cullingMask |= (1 << TransparentWallsPatch.WallLayerMask);//Enables HMD bits because layer 26 is masked by default
            Renderer mesh = __instance.gameObject?.GetComponentInChildren<Renderer>(false);
            if (mesh?.gameObject)
            {
                mesh.gameObject.layer = WallLayerMask;
            }
        }
    }
}
