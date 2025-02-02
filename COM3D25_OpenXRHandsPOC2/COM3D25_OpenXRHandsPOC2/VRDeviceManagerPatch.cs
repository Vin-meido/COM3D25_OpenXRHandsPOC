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


        // patch OvrGripCollider
        [HarmonyLib.HarmonyPatch(typeof(OvrGripCollider), "OnTriggerStay")]
        [HarmonyLib.HarmonyPrefix]
        static void OvrGripCollider_OnTriggerStay_prefix(OvrGripCollider __instance, UnityEngine.Collider col, out bool __state)
        {
            __state = __instance.press_down_trigger_;

            if (__instance.press_down_trigger_)
            {
                var target = __instance.GetTargetTransform(col.gameObject) ?? __instance.GetTargetTransformComm(col.gameObject);
                var enableGrap = __instance.IsEnableGrab(col.gameObject);
                Logger.LogInfo($"<OvrGripCollider>{__instance}.OnTriggerStay: {target} enableGrab: {enableGrap}");
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(OvrGripCollider), "OnTriggerStay")]
        [HarmonyLib.HarmonyPostfix]
        static void OvrGripCollider_OnTriggerStay_postfix(OvrGripCollider __instance, bool __state, UnityEngine.Collider col)
        {
            if (__state)
            {
                Logger.LogInfo($"<OvrGripCollider>{__instance}.OnTriggerStay Exit lock: {__instance.lock_object_trans_}");

            }
        }

        static Harmony harmony;

        public static void Patch() {
            var type = typeof(VRDeviceManagerPatch);
            harmony = new Harmony(type.Name);
            harmony.PatchAll(type);
        }
    }
}
