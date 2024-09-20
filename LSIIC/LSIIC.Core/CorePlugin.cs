using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FistVR;
using HarmonyLib;
using RUST.Steamworks;
using Steamworks;
using Sodalite.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sodalite;

[assembly: AssemblyVersion("1.6.0")]
namespace LSIIC.Core
{
	[BepInPlugin("net.block57.lsiic.core", "LSIIC - Core", "1.6.0")]
	[BepInDependency("dll.cursed.timescale", BepInDependency.DependencyFlags.SoftDependency)]
	public class CorePlugin : BaseUnityPlugin
	{
		public static ManualLogSource Logger { get; set; }

		public static ConfigEntry<bool> _pilotPlayerSosigBodyHead;

		public static ConfigEntry<KeyboardShortcut> _shortcutDeleteFVRPhysicalObjects;
		public static ConfigEntry<KeyboardShortcut> _shortcutPrintLayerAndTagsInfo;
		public static ConfigEntry<KeyboardShortcut> _shortcutPrintAllStreamingAssetsBundles;

		public static ConfigEntry<KeyboardShortcut> _shortcutTelePlayerToOrigin;
		public static ConfigEntry<KeyboardShortcut> _shortcutTelePlayerToReset;
		public static ConfigEntry<KeyboardShortcut> _shortcutTelePlayer2mForward;

		public static ConfigEntry<KeyboardShortcut> _shortcutSpawnModPanelV2;
		public static ConfigEntry<KeyboardShortcut> _shortcutScrambleMaterials;
		public static ConfigEntry<KeyboardShortcut> _shortcutScrambleMeshes;

		public CorePlugin()
		{
			Logger = base.Logger;
			Logger.Log(LogLevel.Debug, "Core pre");

			_pilotPlayerSosigBodyHead = Config.Bind("Functionality", "Pilot PlayerSosigBody Head", true,
				"Pilot the PlayerSosigBody's head with your actual head rotation instead of the head's physics joint.");

			_shortcutDeleteFVRPhysicalObjects = Config.Bind("Keybinds - Debug", "Delete FVRPhysicalObjects", new KeyboardShortcut(KeyCode.P, KeyCode.LeftShift),
				"Deletes all FVRPhysicalObjects that aren't in quickbelts in the scene.");
			_shortcutPrintLayerAndTagsInfo = Config.Bind("Keybinds - Debug", "Print Layer and Tags Info", new KeyboardShortcut(KeyCode.L, KeyCode.LeftShift),
				"Prints all Layers and Tags.");
			_shortcutPrintAllStreamingAssetsBundles = Config.Bind("Keybinds - Debug", "Print All StreamingAssets Bundles", new KeyboardShortcut(KeyCode.A, KeyCode.LeftShift),
				"Prints all prefabs in StreamingAssets, and every ItemSpawnerID.");

			_shortcutTelePlayerToOrigin = Config.Bind("Keybinds - Player", "Teleport Player to Origin", new KeyboardShortcut(KeyCode.Z),
				"Teleports the player rig to the origin of the level.");
			_shortcutTelePlayerToReset = Config.Bind("Keybinds - Player", "Teleport Player to Reset", new KeyboardShortcut(KeyCode.R),
				"Teleports the player rig to the reset point of the level.");
			_shortcutTelePlayer2mForward = Config.Bind("Keybinds - Player", "Teleport Player 2m Forward", new KeyboardShortcut(KeyCode.F),
				"Teleports the player rig 2 meters in whatever direction the player is looking in.");

			_shortcutSpawnModPanelV2 = Config.Bind("Keybinds - Misc", "Spawn ModPanelV2", new KeyboardShortcut(KeyCode.M, KeyCode.LeftAlt),
				"Spawns ModPanelV2 in an empty quickbelt slot, or on the floor if none are free.");
			_shortcutScrambleMaterials = Config.Bind("Keybinds - Misc", "Scramble Materials", new KeyboardShortcut(KeyCode.T, KeyCode.LeftAlt),
				"WARNING: SLOW AND DUMB\nScrambles all materials in the scene.");
			_shortcutScrambleMeshes = Config.Bind("Keybinds - Misc", "Scramble Meshes", new KeyboardShortcut(KeyCode.Y, KeyCode.LeftAlt),
				"WARNING: SLOW AND DUMB\nScrambles all meshes in the scene.");

			//Environment.SetEnvironmentVariable("MONOMOD_DMD_TYPE", "cecil");
			//Environment.SetEnvironmentVariable("MONOMOD_DMD_DUMP", "./mmdump");

			Harmony harmony = Harmony.CreateAndPatchAll(typeof(HarmonyPatches));

			//this removes any other patches to the wrist menu's update function,
			//it's kind of terrible but it's the best method I could think to do
			//based on the types of patches that are made to the wrist menu
			MethodInfo FVRWristMenuUpdate = AccessTools.Method(typeof(FVRWristMenu), nameof(FVRWristMenu.Update));
			if (FVRWristMenuUpdate != null)
			{
				Patches wristUpdatePatches = Harmony.GetPatchInfo(FVRWristMenuUpdate);
				foreach (Patch postfix in wristUpdatePatches.Postfixes)
					if (postfix.owner != harmony.Id)
						harmony.Unpatch(FVRWristMenuUpdate, HarmonyPatchType.All, postfix.owner);
			}

			//Environment.SetEnvironmentVariable("MONOMOD_DMD_DUMP", null);

			SceneManager.sceneLoaded += SceneManager_sceneLoaded;

			Helpers.CachedSceneIndex = SceneManager.GetActiveScene().buildIndex;
			Helpers.CachedSceneName = SceneManager.GetActiveScene().name;
			Helpers.CachedLaunchTime = DateTime.Now.AddSeconds(-Time.realtimeSinceStartup);

			Logger.Log(LogLevel.Debug, "Core post");
		}

		public void Start()
		{
			WristMenuAPI.Buttons.Add(new WristMenuButton("Spawn ModPanelV2", 0, SpawnModPanel));
		}

		public void Update()
		{
			if (Helpers.BepInExGetKeyDown(_shortcutDeleteFVRPhysicalObjects.Value))
			{
				foreach (var phys in FindObjectsOfType<FVRPhysicalObject>())
				{
					if (phys.QuickbeltSlot != null || phys.m_isHardnessed || phys.IsHeld)
						continue;
					Destroy(phys.gameObject);
				}
			}
			else if (Helpers.BepInExGetKeyDown(_shortcutPrintLayerAndTagsInfo.Value))
			{
				string LayerNames = "Layers:";
				for (int i = 0; i < 32; i++)
					LayerNames += $"\n{i} - {LayerMask.LayerToName(i)}";
				Debug.Log(LayerNames);

				string LayerCollisions = "Layer Collisions: (+ for ignoring)\n";
				LayerCollisions += "   |  0|  1|  2|  3|  4|  5|  6|  7|  8|  9| 10| 11| 12| 13| 14| 15| 16| 17| 18| 19| 20| 21| 22| 23| 24| 25| 26| 27| 28| 29| 30| 31|";
				for (int i = 0; i < 32; i++)
				{
					LayerCollisions += $"\n{i,3}|";
					for (int j = 0; j < 32; j++)
						LayerCollisions += !Physics.GetIgnoreLayerCollision(2 ^ i, 2 ^ j) ? " + |" : "   |";
				}
				Debug.Log(LayerCollisions);
			}
			else if (Helpers.BepInExGetKeyDown(_shortcutPrintAllStreamingAssetsBundles.Value))
			{
				string ootpoot = "";

				foreach (string assBundle in System.IO.Directory.GetFiles(Application.streamingAssetsPath))
				{
					if (!assBundle.Contains("."))
					{
						AssetBundle asset = AnvilManager.GetBundleAsync(System.IO.Path.GetFileName(assBundle)).Result;
						if (asset != null)
						{
							string[] names = System.IO.Path.GetFileName(assBundle).Split(new[] { "_" }, StringSplitOptions.None);
							string name = names[names.Length - 1];
							ootpoot += $"{name}: {string.Join($"\n{name}: ", asset.GetAllAssetNames())}\n\n\n";
						}
					}
				}

				foreach (System.Collections.Generic.KeyValuePair<string, ItemSpawnerID> kvp in (System.Collections.Generic.Dictionary<string, ItemSpawnerID>)AccessTools.Field(typeof(IM), "SpawnerIDDic").GetValue(ManagerSingleton<IM>.Instance))
					ootpoot += $"{kvp.Key} - {kvp.Value}\n";

				Debug.Log(ootpoot);
			}

			else if (Helpers.BepInExGetKeyDown(_shortcutTelePlayerToReset.Value))
				GM.CurrentMovementManager.TeleportToPoint(GM.CurrentSceneSettings.DeathResetPoint.position, true);
			else if (Helpers.BepInExGetKeyDown(_shortcutTelePlayer2mForward.Value))
				GM.CurrentMovementManager.TeleportToPoint(GM.CurrentPlayerRoot.position + (GM.CurrentPlayerBody.Head.forward * 2.0f), true);
			else if (Helpers.BepInExGetKeyDown(_shortcutTelePlayerToOrigin.Value))
				GM.CurrentMovementManager.TeleportToPoint(Vector3.zero, true);

			else if (Helpers.BepInExGetKeyDown(_shortcutSpawnModPanelV2.Value))
			{
				ItemSpawnerID id = IM.GetSpawnerID("MiscUtModPanelV2");
				if (id != null)
				{
					GameObject panel = Instantiate(id.MainObject.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerRoot.position, Quaternion.identity);
					if (GM.CurrentPlayerBody.QuickbeltSlots.Count > 0)
					{
						foreach (FVRQuickBeltSlot qbSlot in GM.CurrentPlayerBody.QuickbeltSlots)
						{
							if (qbSlot.HeldObject == null && qbSlot.CurObject == null)
							{
								panel.GetComponent<FVRPhysicalObject>().SetQuickBeltSlot(qbSlot);
								break;
							}
						}
					}
				}
				else
					Logger.LogError("ModPanelV2 was null?");
			}
			else if (Helpers.BepInExGetKeyDown(_shortcutScrambleMaterials.Value))
			{
				foreach (Renderer mr in UnityEngine.Object.FindObjectsOfType(typeof(UnityEngine.Renderer)))
					mr.material = (Resources.FindObjectsOfTypeAll(typeof(Material))[UnityEngine.Random.Range(0, Resources.FindObjectsOfTypeAll(typeof(Material)).Length)] as Material);
			}
			else if (Helpers.BepInExGetKeyDown(_shortcutScrambleMeshes.Value))
			{
				foreach (MeshFilter mf in UnityEngine.Object.FindObjectsOfType(typeof(UnityEngine.MeshFilter)))
					mf.mesh = (Resources.FindObjectsOfTypeAll(typeof(Mesh))[UnityEngine.Random.Range(0, Resources.FindObjectsOfTypeAll(typeof(Mesh)).Length)] as Mesh);
			}
		}

		private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadMode)
		{
			Helpers.CachedSceneName = scene.name;
			Helpers.CachedSceneIndex = scene.buildIndex;
		}

		private void SpawnModPanel(object sender, ButtonClickEventArgs e)
		{
			ItemSpawnerID id = IM.GetSpawnerID("MiscUtModPanelV2");
			if (id != null)
			{
				GameObject panel = Instantiate(id.MainObject.GetGameObject(), e.Hand.Input.Pos, e.Hand.Input.Rot);
				WristMenuAPI.Instance?.m_currentHand.RetrieveObject(panel.GetComponent<FVRPhysicalObject>());
			}
		}

		/*
		 * Skiddie prevention
		 */
		[HarmonyPatch(typeof(HighScoreManager), nameof(HighScoreManager.UpdateScore), new Type[] { typeof(string), typeof(int), typeof(Action<int, int>) })]
		[HarmonyPatch(typeof(HighScoreManager), nameof(HighScoreManager.UpdateScore), new Type[] { typeof(SteamLeaderboard_t), typeof(int) })]
		[HarmonyPrefix]
		public static bool HSM_UpdateScore()
		{
			return false;
		}
	}
}