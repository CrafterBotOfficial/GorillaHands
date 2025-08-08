// todo: rename everything 
using BepInEx.Configuration;

namespace GorillaHands;

public static class Configuration
{
    public static BepInEx.Configuration.ConfigFile Config;

    public static ConfigEntry<float> ArmOffsetMultiplier;
    public static ConfigEntry<float> VelocityMultiplierOnRelease;
    public static ConfigEntry<bool> HandCollisions;

    internal static void Initialize(ConfigFile config)
    {
        Config = config;

        ArmOffsetMultiplier = config.Bind("Multipliers", "Arm Offset", 12f);
        VelocityMultiplierOnRelease = config.Bind("Multipliers", "On Release Mult", 2f);
        HandCollisions = config.Bind("Collisions", "Can hand interact with other surfaces?", true);
    }
}
