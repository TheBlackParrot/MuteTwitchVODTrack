using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MuteTwitchVODTrack.Classes;

public class ObsHelloAuthentication
{
    [JsonProperty("challenge")] public string Challenge { get; set; } = string.Empty;
    [JsonProperty("salt")] public string Salt { get; set; } = string.Empty;
    
    [JsonConstructor]
    public ObsHelloAuthentication() { }
}

public class ObsHelloMessage
{
    [JsonProperty("obsStudioVersion")] public string ObsStudioVersion { get; set; } = string.Empty;
    [JsonProperty("obsWebSocketVersion")] public string ObsWebSocketVersion { get; set; } = string.Empty;
    [JsonProperty("rpcVersion")] public int RpcVersion { get; set; }
    [JsonProperty("authentication")] public ObsHelloAuthentication? Authentication { get; set; }
    
    [JsonConstructor]
    public ObsHelloMessage() { }
}

[JsonObject(MemberSerialization.OptIn)]
public class ObsMessage
{
    [JsonProperty("op")] public int? OpCode { get; set; }
    [JsonProperty("d")] public JObject? DataRaw { get; set; }

    public object? Data
    {
        get
        {
            if (DataRaw == null)
            {
                return null;
            }

            return OpCode switch
            {
                0 => JsonConvert.DeserializeObject<ObsHelloMessage>(DataRaw.ToString()),
                5 => JsonConvert.DeserializeObject<ObsEventMessage>(DataRaw.ToString()),
                _ => null
            };
        }
    }

    [JsonConstructor]
    public ObsMessage() { }
}

public class ObsHelloResponse
{
    [JsonProperty("op")] public const int OP_CODE = 1;
    [JsonProperty("d")] public Dictionary<string, dynamic> Data = new();

    public ObsHelloResponse(string? auth = null)
    {
        Data["rpcVersion"] = 1;
        Data["eventSubscriptions"] = 1 << 3; // 1 << 3 is input events
        
        if (auth != null)
        {
            Data["authentication"] = auth;
        }
    }
}

public class ObsEventMessage
{
    [JsonProperty("eventType")] public string? EventType { get; set; }
    [JsonProperty("eventIntent")] public int? EventIntent { get; set; }
    [JsonProperty("eventData")] public Dictionary<string, dynamic>? EventData { get; set; }
    
    [JsonConstructor]
    public ObsEventMessage() { }
}

public class ObsAudioTracksActive
{
    [JsonProperty("1")] public bool Track1 { get; set; }
    [JsonProperty("2")] public bool Track2 { get; set; }
    [JsonProperty("3")] public bool Track3 { get; set; }
    [JsonProperty("4")] public bool Track4 { get; set; }
    [JsonProperty("5")] public bool Track5 { get; set; }
    [JsonProperty("6")] public bool Track6 { get; set; }

    [JsonIgnore]
    public bool GetActiveVodTrack =>
        Plugin.ActiveVodTrack.Value switch
        {
            1 => Track1,
            2 => Track2,
            3 => Track3,
            4 => Track4,
            5 => Track5,
            6 => Track6,
            _ => throw new InvalidOperationException()
        };
}

public class ObsRequestMessage
{
    [JsonProperty("requestType")] public string? RequestType { get; set; }
    [JsonProperty("requestId")] public string RequestId = Guid.NewGuid().ToString();
    [JsonProperty("requestData")] public Dictionary<string, dynamic> RequestData { get; set; } = new();
    
    [JsonConstructor]
    public ObsRequestMessage() { }
}

public class ObsRequestMessageRoot
{
    [JsonProperty("op")] public const int OP_CODE = 6;
    [JsonProperty("d")] public ObsRequestMessage Data = new();
}