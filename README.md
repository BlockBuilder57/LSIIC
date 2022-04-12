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
	- The first spectator system for H3VR, eventually added into the game in an official capacity. Just exists as a callback to the original.
- Spectator Camera Attachment
	- Above, but as an attachment.
- Portable Grab Point
	- A ladder grab point you can take anywhere, even in the air!

This repo also includes a WurstMod testing map for LSIIC.

## Installation

LSIIC can be installed through any [Thunderstore](https://h3vr.thunderstore.io/package/BlockBuilder57/LSIIC/) compatible mod manager or can be installed manually with the ZIP provided in each release.

## Plugin descriptions

#### `Assembly-CSharp.LSIIC.mm` -- big patches and custom objects

Adds all the additional items to the base game. For example, this is what makes ModPanelV2 work. This also includes a patch to Bangers to allow them to be spawnlocked.

#### `LSIIC.Core` -- the heart of LSIIC

This plugin contains most of the core functionality of LSIIC, including a function for held object information and a few other fun patches. Refer to the config file (`BepInEx/config/net.block57.lsiic.core.cfg`) to see all the keybinds

Some new functionality includes (among other changes):
- Magazines and clips can now go up to 1024x their normal capacity if changed with ModPanelV2.
- If enabled, time will stop with an Armswinger/Twinstick jump.
	- Note that you'll have to use teleport if you wish to move around in frozen time.
	- This gets better with the timescale plugin from the CursedDlls.
- Custom wrist menu with much more information than usual.
- Hover Bench can be picked up even with an object on it.
- Bangers can be spawnlocked.

#### `LSIIC.SmartPalming` -- former plugin

Conceptualized by Jack Foxtrot, smart palming was added in Update 100 Alpha 6.

## Compiling/Developing

The LSIIC plugins can be compiled with any C# compilier, but it has been written using Visual Studio 2019.

The Unity side of LSIIC is found in the H3VRMods folder. To avoid uploading other's work, the only parts of the project included there are the project settings and `Assets/LSIIC` folder. The project requires this specific fork of [Alloy](https://github.com/Josh015/Alloy/tree/unity-5-6/) (the PBR rendering pipeline used in H3VR), and [WurstMod](https://github.com/Nolenz/WurstMod) for the testing area map. Unity's [AssetBundles Browser](https://github.com/Unity-Technologies/AssetBundles-Browser) addon is also used to export the `lsiic` assetbundle in `H3VR/VirtualObjects`, when LSIIC.VirtualObjectsInjector is installed. The project uses [uTinyRipper](https://github.com/mafaca/UtinyRipper) for the .meta files from H3VR. To extract these, drag/select the h3vr_Data folder in your H3VR install folder and export the scripts from the output.
