using UnityEngine.XR;
using BepInEx.Configuration;

namespace GorillaHands;

public static class Configuration
{
    public static BepInEx.Configuration.ConfigFile Config;

    public static ConfigEntry<float> ArmOffsetMultiplier;
    public static ConfigEntry<float> VelocityMultiplierOnRelease;
    public static ConfigEntry<float> FollowForceMultiplier;
    public static ConfigEntry<float> DampingForceMultiplier;

    public static ConfigEntry<bool> HandCollisions;
    public static ConfigEntry<float> HandSpherecastRadius;
    public static ConfigEntry<float> HandStuckDistanceThreshold;

    public static ConfigEntry<ControllerButton> ToggleHandVisiblityButton;

    public static ConfigEntry<float> RotationLerpAmount;
    public static ConfigEntry<float> TransitionSpeed;

    internal static void Initialize(ConfigFile config)
    {
        Config = config;

        ArmOffsetMultiplier = config.Bind("Multipliers", "Arm Offset", 12f);
        VelocityMultiplierOnRelease = config.Bind("Multipliers", "Booster", 2f, "The velocity multiplier for when you stop climbing.");
        FollowForceMultiplier = config.Bind("Multipliers", "Follow Force", 150f, "The force the hand uses to get to the target pos"); // 50
        DampingForceMultiplier = config.Bind("Multipliers", "Damping Mult", 16f); // 8

        HandCollisions = config.Bind("Collisions", "Hand collisions", true, "Can hand interact with other surfaces?");
        HandSpherecastRadius = config.Bind("Collisions", "Spherecast Radius", .55f, "Bigger number equals bigger snap.");
        HandStuckDistanceThreshold = config.Bind("Collisions", "HandStuckDistanceThreshold", 10f, "How fare can the hand get before it will go through walls to return. (Avoids getting stuck in trees)");

        ToggleHandVisiblityButton = config.Bind("Controls", "Toggle hand", ControllerButton.Primary);

        RotationLerpAmount = config.Bind("Misc", "Rotation Lerp Amount", .1f, "Speed that the hands will rotate to match the real player hands.");
        TransitionSpeed = config.Bind("Misc", "Transition Speed", 12f, "The speed the hands will appear/disappear when you press the button.");
    }
}

public enum ControllerButton
{
    Primary,
    Secondary,
    JoystickDown,
}
