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
    public static ConfigEntry<float> RotationLerpAmount;
    public static ConfigEntry<float> TransitionSpeed;

    internal static void Initialize(ConfigFile config)
    {
        Config = config;

        ArmOffsetMultiplier = config.Bind("Multipliers", "Arm Offset", 12f); // todo: investigate this default value, Im not totally convinced its best
        VelocityMultiplierOnRelease = config.Bind("Multipliers", "Booster", 2f, "The velocity multiplier for when you stop climbing.");
        FollowForceMultiplier = config.Bind("Multipliers", "Follow Force", 50f, "The force the hand uses to get to the target pos");
        DampingForceMultiplier = config.Bind("Multipliers", "Damping Mult", 8f);

        HandCollisions = config.Bind("Collisions", "Hand collisions", true, "Can hand interact with other surfaces?");
        HandSpherecastRadius = config.Bind("Collisions", "Spherecast Radius", .35f);

        HandStuckDistanceThreshold = config.Bind("Misc", "HandStuckDistanceThreshold", 15f, "How fare can the hand get before it teleports back.");
        RotationLerpAmount = config.Bind("Misc", "Rotation Lerp Amount", .1f, "Speed that the hands will rotate to match the real player hands.");
        TransitionSpeed = config.Bind("Misc", "Transition Speed", 8f, "The speed the hands will appear/disappear when you press the button.");
    }
}
