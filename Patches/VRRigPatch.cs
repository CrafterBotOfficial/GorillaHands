// using HarmonyLib;
// using UnityEngine;
//
// namespace GorillaHands.Patches;
//
// [HarmonyPatch(typeof(VRRig), nameof(VRRig.SetColor))]
// public static class VRRigPatch
// {
//     [HarmonyPostfix]
//     private static void OnColorChanged(VRRig __instance)
//     {
//         if (!__instance.isOfflineVRRig) return;
//         Main.instance.rightHand?.UpdateColor();
//         Main.instance.leftHand?.UpdateColor();
//     }
// }
