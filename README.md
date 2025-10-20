# MuteTwitchVODTrack
**Mod for Spin Rhythm XD that will automatically toggle an audio track on an audio input source in OBS when selecting songs in the menu and playing maps.**

## Configuration
Starting the game once will automatically generate a configuration file (`Spin Rhythm/BepInEx/config/MuteTwitchVODTrack.cfg`) that can be edited after closing the game. Set the values accordingly, then restart the game.  

If everything is correct and a connection can be established, toggling the setting in the added side panel menu should now change the state of the audio track defined in the configuration file.  

Audible songs are saved in a JSON file (as an array of file references) in `Spin Rhythm/BepInEx/config/MuteTwitchVODTrack_AudibleList.json`.

## Dependencies
- SpinCore
- websocket-sharp *(included)*
- Any version of OBS equal to or newer than 28.0.0
  - *(older versions may work if you use a compatible version of the v5 WebSocket plugin)*

---

**Project includes source files from [websocket-sharp](https://github.com/sta/websocket-sharp/tree/01a1a7559f21e38af1045a1ae1e8c123416b6df3), licensed under MIT**