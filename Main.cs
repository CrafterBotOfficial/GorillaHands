﻿using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using Utilla.Attributes;

namespace GorillaHands;

[BepInPlugin("com.crafterbot.gorillahands", "GorillaHands", "1.0.1.1")]
[BepInDependency("org.legoandmars.gorillatag.utilla", "1.6.0")]
[ModdedGamemode]
public class Main : BaseUnityPlugin
{
    private static Main instance;

    public static UnityEngine.Object leftHandPrefab, rightHandPrefab;
    public HandController
        leftHand,
        rightHand;

    private void Awake()
    {
        instance = this;
        Configuration.Initialize(Config);
        Utilla.Events.GameInitialized += async (_, _) =>
        {
            Log("Creating hands");

            using var assetLoader = new AssetLoader("GorillaHands.Resources.hands");
            leftHandPrefab = await assetLoader.LoadAsset("LeftHand");
            rightHandPrefab = await assetLoader.LoadAsset("RightHand");

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
