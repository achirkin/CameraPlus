using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraPlus
{
    [HarmonyPatch(typeof(StretchableCube))]
    [HarmonyPatch("Awake", MethodType.Normal)]
    public class TransparentWallsPatch
    {
        public static int WallLayerMask = 25;
        public static void Postfix(StretchableCube __instance)
        {
            __instance.gameObject.layer = WallLayerMask;
        }
    }
}
