using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CameraPlus
{
    [HarmonyPatch(typeof(WindowOverride))]
    [HarmonyPatch("WndProc", MethodType.Normal)]
    public class WindowOverridePatch
    {
        private static readonly int WM_RBUTTONDOWN = 516;
        private static readonly int WM_RBUTTONUP = 517;

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "CallWindowProcW")]
        private static extern IntPtr CallWindowProc(IntPtr prevWndProc, IntPtr hwnd, uint msg, IntPtr wparam, IntPtr lparam);

        public static bool Prefix(WindowOverride __instance, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref IntPtr __result, IntPtr ____oldWndProcPtr)
        {
            if (msg == WM_RBUTTONDOWN || msg == WM_RBUTTONUP)
            {
                __result = CallWindowProc(____oldWndProcPtr, hWnd, msg, wParam, lParam);
                return false;
            }
            return true;
        }
    }
}
