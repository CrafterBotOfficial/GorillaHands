using BepInEx;
using BepInEx.Logging;
using Jerald;
using UnityEngine;
using Utilla.Attributes;

[assembly: AutoRegister]

namespace GorillaHands;

[BepInPlugin("com.crafterbot.gorillahands", "GorillaHands", "1.0.0")]
[BepInDependency("org.legoandmars.gorillatag.utilla", "1.6.0")]
[BepInDependency("crafterbot.gorillatag.computer")]
[ModdedGamemode]
public class Main : BaseUnityPlugin
{
    private static Main instance;

    public static UnityEngine.Object HandPrefab;
    private HandController
        leftHand,
        rightHand;

    private void Awake()
    {
        instance = this;
        Configuration.Initialize(Config);
        Utilla.Events.GameInitialized += async (sender, args) =>
        {
            Log("Creating hands");

            var assetLoader = new AssetLoader("GorillaHands.Resources.hands");
            HandPrefab = await assetLoader.LoadAsset("WhiteHand");

            rightHand = new GameObject("Hand Controllers").AddComponent<HandController>();
            leftHand = rightHand.gameObject.AddComponent<HandController>();
            leftHand.IsLeft = true;
            OnLeave();
        };
    }

    [ModdedGamemodeJoin]
    private void OnJoin()
    {
        leftHand.enabled = true;
        rightHand.enabled = true;
    }

    [ModdedGamemodeLeave]
    private void OnLeave()
    {
        leftHand.enabled = false;
        rightHand.enabled = false;
    }

    public static void Log(object message, LogLevel level = LogLevel.Info)
    {
        instance?.Logger.Log(level, message);
    }
}
