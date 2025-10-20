using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MuteTwitchVODTrack.Services;
using Newtonsoft.Json;
using SpinCore.UI;
using UnityEngine;
using Axis = SpinCore.UI.Axis;
using Image = UnityEngine.UI.Image;

namespace MuteTwitchVODTrack.UI;

internal static class StatusMenu
{
    internal static CustomSidePanel? StatusMenuPanel;
    internal static CustomMultiChoice? AudibleToggle;

    internal static CustomTextComponent? IsConnectedLabel;
    internal static CustomTextComponent? IsNotConnectedLabel;
    
    internal static bool IsAudible;

    internal static void UpdateToggle()
    {
        AudibleToggle?.SetCurrentValue(IsAudible ? 1 : 0);
        
        Transform? iconObject = GameObject.Find("Dot Selector Button StatusMenuPanel")?.transform.Find("IconContainer/Icon");
        if (iconObject?.TryGetComponent(out Image imageComponent) ?? false)
        {
            imageComponent.sprite = IsAudible ? _audibleSprite : _mutedSprite;
        }
    }

    private static Sprite? _audibleSprite;
    private static Sprite? _mutedSprite;
    
    internal static void CreateQueueListPanel()
    {
        // this has to be in a Task.Run instead of just making this an async Task. do i know why? Nope
        Task.Run(async () =>
        {
            await Awaitable.MainThreadAsync();

            _audibleSprite = Resources.FindObjectsOfTypeAll<Sprite>().First(x => x.name == "Mute OFF");
            _mutedSprite = Resources.FindObjectsOfTypeAll<Sprite>().First(x => x.name == "Mute ON");
            
            StatusMenuPanel = UIHelper.CreateSidePanel(nameof(StatusMenuPanel), $"{nameof(MuteTwitchVODTrack)}_PanelHeaderText", _mutedSprite);
            StatusMenuPanel.OnSidePanelLoaded += StatusMenuPanelOnSidePanelLoaded;
            
            //CheckIndicatorDot();
        });
    }

    private static void StatusMenuPanelOnSidePanelLoaded(Transform panelTransform)
    {
        StatusMenuPanel!.OnSidePanelLoaded -= StatusMenuPanelOnSidePanelLoaded;

        CustomGroup statusMenuConnectionIndicatorGroup =
            UIHelper.CreateGroup(panelTransform, "StatusMenuConnectionIndicatorGroup");
        statusMenuConnectionIndicatorGroup.LayoutDirection = Axis.Horizontal;
        
        IsConnectedLabel = UIHelper.CreateLabel(statusMenuConnectionIndicatorGroup,
            "StatusMenuIsConnectedLabel", $"{nameof(MuteTwitchVODTrack)}_IsConnected");
        IsNotConnectedLabel = UIHelper.CreateLabel(statusMenuConnectionIndicatorGroup,
            "StatusMenuIsNotConnectedLabel", $"{nameof(MuteTwitchVODTrack)}_IsNotConnected");
        IsConnectedLabel.GameObject.SetActive(ObsConnection.IsSuccessfullyConnected);
        IsNotConnectedLabel.GameObject.SetActive(!ObsConnection.IsSuccessfullyConnected);
        
        CustomGroup statusMenuOptionsGroup = UIHelper.CreateGroup(panelTransform, "StatusMenuOptionsGroup");
        statusMenuOptionsGroup.LayoutDirection = Axis.Horizontal;
        AudibleToggle = UIHelper.CreateSmallToggle(statusMenuOptionsGroup, "IsAudibleToggle",
            $"{nameof(MuteTwitchVODTrack)}_{nameof(IsAudible)}", IsAudible, value =>
            {
                if (XDSelectionListMenu.Instance._previewTrackDataSetup.Item1 == null)
                {
                    return;
                }
                
                string safeFileReference =
                    Plugin.GetSafeFileReferenceString(XDSelectionListMenu.Instance._previewTrackDataSetup.Item1);

                bool changed = false;
                if (Plugin.ReferenceList.Contains(safeFileReference) && !value)
                {
                    changed = true;
                    Plugin.ReferenceList.Remove(safeFileReference);
                }
                if(!Plugin.ReferenceList.Contains(safeFileReference) && value)
                {
                    changed = true;
                    Plugin.ReferenceList.Add(safeFileReference);
                }

                if (changed)
                {
                    File.WriteAllText(Plugin.ReferenceListPath, JsonConvert.SerializeObject(Plugin.ReferenceList));
                }

                IsAudible = value;
                _ = ObsConnection.SendVodAudibleStatus();
            });
        UpdateToggle();
        
        //CheckIndicatorDot();
    }
}