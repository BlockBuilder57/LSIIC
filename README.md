# LSIIC - Let's See If It Crashes

Here's that weird [Hot Dogs, Horseshoes, and Hand Grenades](http://h3vr.com) mod I've been working on since late 2017 or so, recently ported to BepInEx. Some older parts of LSIIC have moved to the [CursedDlls](https://github.com/drummerdude2003/CursedDlls.BepinEx/) project.

## Features

- ModPanelV2
	- A redone version of the old version's ModPanel, now authored entirely in Unity.
	- Pages:
		- Portable Item Spawner
		- Held Object Modifier
		- Scene Settings and Options
		- Damage Report/Rate of Fire Tester
        - Debug Information
- COOL GUNS
	- COOLREVOLVER
		- The first ever custom gun in H3VR, this is the COOLEST REVOLVER EVER MADE!!!1!
    - COOLCLOSEDBOLT
		- A more feature-packed alternative to the MP7A1, this is the COOLEST CLOSED BOLT EVER MADE!!!1!
- env_cubemap
	- The first ever custom melee weapon for H3VR, doubles as a cubemap viewer.
- New Flagtachments
	- Fun flag(s) to attach on your gun.
- Spectator Camera
	- The first spectator system for H3VR, eventually added into the game officially when systems were ready.
- Spectator Camera Attachment
	- Above, but as an attachment.
- Portable Grab Point
	- A ladder grab point you can take anywhere!

This repo also includes a WurstMod testing map for LSIIC.

## Installation

Requirements are:

* [BepInEx 5.2 or newer](https://github.com/BepInEx/BepInEx)
* [BepInEx.MonoMod.Loader](https://github.com/BepInEx/BepInEx.MonoMod.Loader)
* [H3VR.Sideloader](https://github.com/denikson/H3VR.Sideloader)

Optionally:

* [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager)
* [RuntimeUnityEditor](https://github.com/ManlyMarco/RuntimeUnityEditor) (for at-keyboard object editing)
* [WurstMod](https://github.com/Nolenz/WurstMod) (for Generic-LSIICTestingArea)

To install, do the following:

1. Grab latest version of BepInEx [from releases](https://github.com/BepInEx/BepInEx/releases). Pick the `x64` version.
2. Extract the downloaded zip into H3VR game folder (`<STEAM folder>/steamapps/common/H3VR`) so that `winhttp.dll` is next to `h3vr.exe`
      * It's recommended that you run the game now *at least once*. That way BepInEx initializes all the folders and configuration files.
      * *Optional* Enable the debug console by opening `<H3VR folder>/BepInEx/config/BepInEx.cfg`, finding and setting
		```toml
		[Logging.Console]

		Enabled = true
		```
3. Download the latest LSIIC release from the releases page.
4. Open the downloaded zip. Extract the downloaded zip into your **H3VR** folder. If you did it correctly, you should now have an `LSIIC` folder in the `BepInEx/plugins` folder, and a `VirtualObjects` folder in your H3VR folder.
5. Download BepInEx.MonoMod.Loader from its [releases](https://github.com/BepInEx/BepInEx.MonoMod.Loader/releases) and extract the zip into your **H3VR** folder. If you did it correctly, you should now have a `monomod` folder in `BepInEx`.
6. To properly install H3VR.Sideloader, follow the [installation instructions](https://github.com/denikson/H3VR.Sideloader/#installation) in its repo. Be sure you have the BepIn 5x version of XUnity.ResourceRedirector as linked in the repo.

Optional Instructions:

1. Download BepInEx.ConfigurationManager from its [releases](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases) and extract the zip into your **H3VR** folder. If you did it correctly, you should now have `ConfigurationManager.dll` file in the `BepInEx/plugins` folder.
2. Download RuntimeUnityEditor from its [releases](https://github.com/ManlyMarco/RuntimeUnityEditor/releases). *Be sure* to get the BepIn 5.x version, and extract the zip into your **H3VR** folder. If you did it correctly, you should now have a `RuntimeUnityEditor` folder in the `BepInEx/plugins` folder.
3. Download WurstMod from its [releases](https://github.com/Nolenz/WurstMod/releases) and extract the zip into your **H3VR** folder. If you did it correctly, you should now have a `WurstMod` folder in the `BepInEx/plugins` folder, and a `CustomLevels` folder in your H3VR folder.

## Plugin descriptions

#### `Assembly-CSharp.LSIIC.mm` -- custom object functionality

Adds all the additional functionality to the base game. For example, this is what makes ModPanelV2 work.

#### `LSIIC.Core` -- the heart of LSIIC

This plugin contains most of the core functionality of LSIIC, including printing game information and a few other fun patches. Refer to the config file (`BepInEx/config/net.block57.lsiic.core.cfg`) to see all the keybinds

Some new functionality includes (among other changes):
- Magazines and clips can now go up to 1024x their normal capacity if changed with ModPanelV2.
- If enabled, time will stop with an Armswinger/Twinstick jump.
	- Note that you'll have to use teleport if you wish to move around in frozen time.
	- This gets better with the timescale plugin from the CursedDlls.
- Custom wrist menu with much more information than usual.
- Hover Bench can be picked up even with an object on it.
- Bangers can be spawnlocked.

#### `LSIIC.SmartPalming` -- smarter round palming

Conceptualized by Jack Foxtrot, smart palming will only take as many rounds as needed to fill your magazines/clip/gun. 

#### `LSIIC.VirtualObjectsInjector` -- inject custom objects into the game

This plugin is now a part of [H3VR.Sideloader](https://github.com/denikson/H3VR.Sideloader/). However, it is still in the LSIIC repo for its hassle-free rapid prototyping. While included in releases, I'd recommend that object makers make Sideloader or OtherLoader mods for easier distribution.

## Compiling/Developing

The LSIIC plugins can be compiled with any C# compilier, but it has been written under Visual Studio 2019 with some MSBuild customization written by [modeco80](https://github.com/modeco80). These custom MSBuild scripts will automatically copy the built dlls to the average H3VR install folder. (when Steam is in `C:\Program Files (x86)`) If needed, line 17 of [LSIIC.CopyToGame.targets](LSIIC/LSIIC.CopyToGame.targets) can be changed to your local install folder. Check the [README](lib/README.md) in /lib to see what dependencies are needed for a successful compilation.

The Unity side of LSIIC is found in the H3VRMods folder. To avoid uploading other's work, the only parts of the project included there are the project settings and `Assets/LSIIC` folder. The project requires this specific fork of [Alloy](https://github.com/Josh015/Alloy/tree/unity-5-6/) (the PBR rendering pipeline used in H3VR), and [WurstMod](https://github.com/Nolenz/WurstMod) for the testing area map. Unity's [AssetBundles Browser](https://github.com/Unity-Technologies/AssetBundles-Browser) addon is also used to export the `lsiic` assetbundle in `H3VR/VirtualObjects`, when LSIIC.VirtualObjectsInjector is installed. The project uses [uTinyRipper](https://github.com/mafaca/UtinyRipper) for the .meta files from H3VR. To extract these, drag/select the h3vr_Data folder in your H3VR install folder and export the scripts from the output.
