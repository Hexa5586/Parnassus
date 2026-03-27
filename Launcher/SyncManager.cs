using System.Text.Json;
using System.Text.Json.Serialization;

namespace Parnassus.Launcher;

/// <summary>
/// Sync the in-game blacklist to blacklist library.
/// </summary>
public class SyncManager
{
    private readonly string configDir;
    private readonly string actualBlacklistDir;
    private readonly string blacklistsDir;

    public SyncManager(string _configDir, string _actualBlacklistDir, string _blacklistsDir)
    {
        configDir = _configDir;
        actualBlacklistDir = _actualBlacklistDir;
        blacklistsDir = _blacklistsDir;
    }

    /// <summary>
    /// Record the given profile name to Parnassus/config.json.
    /// </summary>
    /// <param name="profileName">The name of the profile (without extension)</param>
    public void Record(string profileName)
    {
        var data = new LastLaunchConfig
        {
            LastProfile = profileName
        };
        string json = JsonSerializer.Serialize(data, AppJsonContext.Default.LastLaunchConfig);
        File.WriteAllText(configDir, json);
    }

    /// <summary>
    /// Read the profile name used to launch Everest last time in Parnassus/config.json, and
    /// sync the profile with Mods/blacklist.txt.
    /// </summary>
    /// <returns>
    /// 0: Succesfully synced;
    /// -1: Failed syncing.
    /// </returns>
    public int SyncLatestLaunchedBlacklist()
    {
        if (!File.Exists(configDir))
        {
            File.WriteAllText(configDir, "{}");
        }

        try
        {
            var json = File.ReadAllText(configDir);
            var config = JsonSerializer.Deserialize(json, AppJsonContext.Default.LastLaunchConfig);
            string? lastProfile = config?.LastProfile;

            if (string.IsNullOrEmpty(lastProfile))
            {
                return -1;
            }

            string targetProfilePath = Path.Combine(blacklistsDir, $"{lastProfile}.txt");

            File.Copy(actualBlacklistDir, targetProfilePath, true);
            return 0;
        }
        catch (Exception)
        {
            return -1;
        }
    }

}

[JsonSerializable(typeof(LastLaunchConfig))]
internal partial class AppJsonContext : JsonSerializerContext
{
}

public class LastLaunchConfig
{
    public string? LastProfile { get; set; }
}