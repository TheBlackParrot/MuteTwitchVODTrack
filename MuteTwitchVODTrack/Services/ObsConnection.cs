using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MuteTwitchVODTrack.Classes;
using MuteTwitchVODTrack.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace MuteTwitchVODTrack.Services;

internal abstract class ObsConnection
{
    private static bool _isSuccessfullyConnected;
    internal static bool IsSuccessfullyConnected
    {
        get => _isSuccessfullyConnected;
        private set
        {
            _isSuccessfullyConnected = value;
            
            StatusMenu.IsConnectedLabel?.GameObject.SetActive(value);
            StatusMenu.IsNotConnectedLabel?.GameObject.SetActive(!value);
        }
    }

    private static WebSocket? _webSocket;
    
    internal static void Initialize()
    {
        if (_webSocket != null)
        {
            return;
        }
        
        _webSocket = new WebSocket($"ws://{Plugin.ObsAddress.Value}:{Plugin.ObsPort.Value}", "obswebsocket.json");

        _webSocket.OnOpen += OnOpen;
        _webSocket.OnError += OnError;
        _webSocket.OnMessage += OnMessage;
        _webSocket.OnClose += OnClose;
        
        _webSocket.ConnectAsync();
    }
    
    private static void OnOpen(object sender, EventArgs e)
    {
        Plugin.Log.LogInfo("Connected to OBS");
    }
    
    private static void OnClose(object sender, CloseEventArgs e)
    {
        Plugin.Log.LogInfo($"Connection to OBS closed: {e.Reason} (code {e.Code})");
        
        _webSocket = null;
        IsSuccessfullyConnected = false;

        switch (e.Code)
        {
            case 1001: // server stopped
            case 1006: // connection was refused (usually not started yet)
                Task.Run(async () =>
                {
                    Plugin.Log.LogWarning("Attempting to reconnect to OBS in 30 seconds...");
                    await Task.Delay(30000);
                    Initialize();
                });
                break;
        }
    }

    private static void OnError(object sender, ErrorEventArgs e)
    {
        Plugin.Log.LogError($"Error thrown while doing something with the WebSocket: {e.Message}");
        throw e.Exception;
    }

    private static void OnMessage(object sender, MessageEventArgs e)
    {
#if DEBUG
        Plugin.Log.LogInfo($"Message received: {e.Data}");
#endif
        
        ObsMessage? message = JsonConvert.DeserializeObject<ObsMessage>(e.Data);

        switch (message?.OpCode)
        {
            case 0:
                HandleHelloMessage(message.Data as ObsHelloMessage);
                break;
            
            case 2:
                Plugin.Log.LogInfo("Identification was successful");
                IsSuccessfullyConnected = true;
                break;
            
            case 5:
                HandleEventMessage(message.Data as ObsEventMessage);
                break;
            
            default:
#if DEBUG
                Plugin.Log.LogInfo($"OpCode {message?.OpCode} is not handled");
#endif
                break;
        }
    }

    // https://github.com/obsproject/obs-websocket/blob/master/docs/generated/protocol.md#creating-an-authentication-string
    private static void HandleHelloMessage(ObsHelloMessage? message)
    {
        if (_webSocket == null)
        {
            return;
        }
        if (message == null)
        {
            Plugin.Log.LogInfo("Received null hello message?");
            return;
        }

        string? final = null;
        if (message.Authentication == null)
        {
            Plugin.Log.LogInfo("No authentication needed");
            goto finish;
        }
        
        string salted = Plugin.ObsPassword.Value + message.Authentication.Salt;
        string secret = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.ASCII.GetBytes(salted)));
        string challenged = secret + message.Authentication.Challenge;
        final = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.ASCII.GetBytes(challenged)));
        
        finish:
            _webSocket.Send(JsonConvert.SerializeObject(new ObsHelloResponse(final)));
    }
    
    private static void HandleEventMessage(ObsEventMessage? message)
    {
        if (message == null)
        {
            Plugin.Log.LogInfo("Received null event message");
            return;
        }
        if (message.EventData == null)
        {
            Plugin.Log.LogInfo("Event message data is null");
            return;
        }

        /*Plugin.Log.LogInfo("Event data contents:");
        foreach (KeyValuePair<string, dynamic> data in message.EventData)
        {
            Plugin.Log.LogInfo($"{data.Key}: {data.Value}");
        }*/

        switch (message.EventType)
        {
            case "InputAudioTracksChanged":
                JObject? audioTrackObject = message.EventData["inputAudioTracks"] as JObject;
                string? audioInputName = message.EventData["inputName"] as string;
                
                if (audioInputName != Plugin.AudioInputName.Value)
                {
#if DEBUG
                    Plugin.Log.LogInfo($"Audio source doesn't match: {audioInputName}, {Plugin.AudioInputName.Value}");
#endif
                    break;
                }

#if DEBUG
                Plugin.Log.LogInfo($"Audio source matched: {audioInputName}");
#endif
                
                ObsAudioTracksActive active =
                    JsonConvert.DeserializeObject<ObsAudioTracksActive>(audioTrackObject?.ToString() ?? throw new NullReferenceException()) ?? throw new NullReferenceException();
                
#if DEBUG
                Plugin.Log.LogInfo($"Track {Plugin.ActiveVodTrack.Value} {(active.GetActiveVodTrack ? "is audible" : "is not audible")}");
#endif
                StatusMenu.IsAudible = active.GetActiveVodTrack;
                StatusMenu.UpdateToggle();
                break;
        }
    }

    public static void SendVodAudibleStatus()
    {
        ObsRequestMessageRoot request = new()
        {
            Data =
            {
                RequestType = "SetInputAudioTracks",
                RequestData =
                {
                    ["inputName"] = Plugin.AudioInputName.Value,
                    ["inputAudioTracks"] = new Dictionary<string, bool>
                    {
                        { Plugin.ActiveVodTrack.Value.ToString(), StatusMenu.IsAudible }
                    }
                }
            }
        };
        
#if DEBUG
        string serialized = JsonConvert.SerializeObject(request);
        Plugin.Log.LogInfo(serialized);
        _webSocket?.Send(serialized);
#else
        _webSocket?.Send(JsonConvert.SerializeObject(request));
#endif
    }
}