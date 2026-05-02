using HarmonyLib;

namespace GorillaHands.Patches;

[HarmonyPatch(typeof(GorillaTagger), nameof(GorillaTagger.UpdateColor))]
public static class GorillaTaggerPatch
{
    [HarmonyWrapSafe]
    private static void Postfix()
    {
        Main.Log("Updating hand color");
        Main.Instance.LeftHand?.UpdateColor();
        Main.Instance.RightHand?.UpdateColor();
    }
}
