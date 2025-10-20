using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MuteTwitchVODTrack.Services;
using MuteTwitchVODTrack.UI;
using Newtonsoft.Json;
using SpinCore.Translation;
using File = System.IO.File;

namespace MuteTwitchVODTrack;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("srxd.raoul1808.spincore", "1.1.2")]
public partial class Plugin : BaseUnityPlugin
{
    // ReSharper disable once MemberCanBePrivate.Global (resharper wtf)
    internal static ManualLogSource Log = null!;
    
    internal static readonly string ReferenceListPath = Path.Combine(Paths.ConfigPath, $"{nameof(MuteTwitchVODTrack)}_AudibleList.json");
    internal static List<string> ReferenceList = null!;
    
    private static readonly Harmony HarmonyInstance = new(MyPluginInfo.PLUGIN_GUID);

    private void Awake()
    {
        Log = Logger;
        
        RegisterConfigEntries();
        ObsConnection.Initialize();
        
        TranslationHelper.AddTranslation($"{nameof(MuteTwitchVODTrack)}_PanelHeaderText", "Twitch VOD Track Status");
        TranslationHelper.AddTranslation($"{nameof(MuteTwitchVODTrack)}_{nameof(StatusMenu.IsAudible)}", "Is audible in VOD");
        
        if (File.Exists(ReferenceListPath))
        {
            ReferenceList = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(ReferenceListPath)) ?? [];
        }
        else
        {
            ReferenceList = [];
        }
        
        StatusMenu.CreateQueueListPanel();
        
        Track.OnStartedPlayingTrack += TrackOnStartedPlayingTrack;
        
        Log.LogInfo("Plugin loaded");
    }

    public void OnEnable()
    {
        HarmonyInstance.PatchAll();
    }
    public void OnDisable()
    {
        HarmonyInstance.UnpatchSelf();
    }

    public static string GetSafeFileReferenceString(MetadataHandle metadata)
    {
        string reference = metadata.UniqueName;
        if (reference.LastIndexOf('_') != -1)
        {
            reference = reference.Remove(metadata.UniqueName.LastIndexOf('_'));
        }

        return reference.Replace("CUSTOM_", string.Empty);
    }

    public static void CheckIfVodShouldMute(MetadataHandle metadata)
    {
        if (string.IsNullOrEmpty(metadata.UniqueName))
        {
            ToggleMute(false);
            return;
        }
                    
        string reference = GetSafeFileReferenceString(metadata);

        // we want this to mute if it's not in the audible list
        ToggleMute(ReferenceList.Contains(reference));
    }

    private static void ToggleMute(bool state)
    {
        StatusMenu.IsAudible = state;
        StatusMenu.UpdateToggle();
        
        if (StatusMenu.AudibleToggle == null)
        {
            // in case the menu hasn't initialized yet
            ObsConnection.SendVodAudibleStatus();
        }
    }

    private static void TrackOnStartedPlayingTrack(PlayableTrackDataHandle handle, PlayState[] _)
    {
        CheckIfVodShouldMute(handle.setup.TrackDataSegmentForSingleTrackDataSetup.metadata);
    }
}