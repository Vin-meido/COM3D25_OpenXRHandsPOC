using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnuVRControl;
using HarmonyLib;

namespace COM3D25_OpenXRHandsPOC2
{

    internal class VRDeviceManagerPatch
    {
        // patch VRDeviceManager
        // private static VRDevice CreateHandControllerDeviceInfo(UnityEngine.InputSystem.InputDevice device)
        [HarmonyLib.HarmonyPatch(typeof(VRDeviceManager), "CreateVRDeviceInfo")]
        [HarmonyLib.HarmonyPrefix]
        static bool CreateHandControllerDeviceInfo_prefix(ref VRDevice __result, UnityEngine.InputSystem.InputDevice device)
        {
            Logger.LogInfo($"CreateHandControllerDeviceInfo: {device.name}");

            if (device.name.Contains("XRHandDevice"))
            {
                __result = new HandTrackingController(device);
                //GameMain.Instance.VRDeviceTypeID = GameMain.VRDeviceType.RIFT_TOUCH;
                return false;
            }
            return true;
        }

        static Harmony harmony;

        public static void Patch() {
            var type = typeof(VRDeviceManagerPatch);
            harmony = new Harmony(type.Name);
            harmony.PatchAll(type);
        }
    }
}
