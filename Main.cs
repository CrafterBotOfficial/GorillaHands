using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Utilla.Attributes;

namespace GorillaHands;

[BepInPlugin("com.crafterbot.gorillahands", "GorillaHands", "1.0.4")]
[BepInDependency("org.legoandmars.gorillatag.utilla", "1.6.0")]
[ModdedGamemode]
public class Main : BaseUnityPlugin
{
    public static Main Instance;

    public static UnityEngine.Object leftHandPrefab, rightHandPrefab;
    public HandController
        LeftHand,
        RightHand;

    private void Awake()
    {
        Instance = this;
        Configuration.Initialize(Config);
        Harmony.CreateAndPatchAll(typeof(Main).Assembly);
        Utilla.Events.GameInitialized += async (_, _) =>
        {
            Log("Creating hands");

            using var assetLoader = new AssetLoader("GorillaHands.Resources.hands");
            leftHandPrefab = await assetLoader.LoadAsset("LeftHand");
            rightHandPrefab = await assetLoader.LoadAsset("RightHand");

            RightHand = new GameObject("Hand Controllers").AddComponent<HandController>();
            LeftHand = RightHand.gameObject.AddComponent<HandController>();
            LeftHand.IsLeft = true;

            OnLeave();
        };
    }

    [ModdedGamemodeJoin]
    private void OnJoin()
    {
        LeftHand.enabled = true;
        RightHand.enabled = true;
    }

    [ModdedGamemodeLeave]
    private void OnLeave()
    {
        LeftHand.enabled = false;
        RightHand.enabled = false;
    }

    public static void Log(object message, LogLevel level = LogLevel.Info)
    {
        Instance.Logger.Log(level, message);
    }
}
