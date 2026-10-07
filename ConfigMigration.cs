using BepInEx.Configuration;

namespace SprocketThermalSight;

internal static class ConfigMigration
{
    // Retain the old file for rollback. An existing neutral file always wins.
    internal static void Import(ConfigFile config)
    {
        string destination = config.ConfigFilePath;
        string legacy = Path.Combine(Path.GetDirectoryName(destination)!,
            "nl.roan.sprocket.thermalsight.cfg");
        if (File.Exists(destination) || !File.Exists(legacy)) return;
        File.Copy(legacy, destination, overwrite: false);
        config.Reload();
    }
}
