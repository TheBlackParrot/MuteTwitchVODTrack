using BepInEx.Configuration;

namespace MuteTwitchVODTrack;

public partial class Plugin
{
    internal static ConfigEntry<string> ObsAddress = null!;
    internal static ConfigEntry<int> ObsPort = null!;
    internal static ConfigEntry<string> ObsPassword = null!;
    
    internal static ConfigEntry<string> AudioInputName = null!;
    internal static ConfigEntry<int> ActiveVodTrack = null!;

    public void RegisterConfigEntries()
    {
        ObsAddress = Config.Bind("OBS", "Address", "127.0.0.1", 
            "IP address of OBS's WebSocket interface");
        ObsPort = Config.Bind("OBS", "Port", 4455, 
            "Port of OBS's WebSocket interface");
        ObsPassword = Config.Bind("OBS", "Password", string.Empty, 
            "Password for OBS's WebSocket interface (leave empty if no password is needed)");
        
        AudioInputName = Config.Bind("General", "AudioInputName", "Game Capture Audio Source",
            "Audio input source name for your game audio");
        ActiveVodTrack = Config.Bind("General", "ActiveVODTrack", 2,
            "Audio track for the Twitch VOD");
    }
}