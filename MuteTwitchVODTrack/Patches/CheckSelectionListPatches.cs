using HarmonyLib;

namespace MuteTwitchVODTrack.Patches;

[HarmonyPatch]
internal class CheckSelectionListPatches
{
    private static string _lastUniqueName = string.Empty;
    internal static MetadataHandle? PreviousMetadataHandle;
    
    [HarmonyPatch(typeof(XDSelectionListMenu), nameof(XDSelectionListMenu.UpdatePreviewHandle))]
    [HarmonyPostfix]
    // ReSharper disable once InconsistentNaming
    public static void XDSelectionListMenu_UpdatePreviewHandlePatch(XDSelectionListMenu __instance)
    {
        if (__instance._previewTrackDataSetup.Item1 == null)
        {
            return;
        }
        if (_lastUniqueName == __instance._previewTrackDataSetup.Item1.UniqueName)
        {
            return;
        }
        
        PreviousMetadataHandle = __instance._previewTrackDataSetup.Item1;
        
        _lastUniqueName = __instance._previewTrackDataSetup.Item1.UniqueName;
        Plugin.CheckIfVodShouldMute(__instance._previewTrackDataSetup.Item1);
    }
}