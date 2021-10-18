using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FistVR;
using HarmonyLib;
using Sodalite.Api;
using System;
using System.Reflection;
using UnityEngine;

[assembly: AssemblyVersion("1.4")]
namespace LSIIC.SmartPalming
{
	[BepInPlugin("net.block57.lsiic.smartpalming", "LSIIC - Smart Palming", "1.4")]
	public class SmartPalmingPlugin : BaseUnityPlugin
	{
		public static ManualLogSource Logger { get; set; }

		public static ConfigEntry<bool> _enableSmartPalming;
		public static ConfigEntry<bool> _addPlusOneForChamber;

		public SmartPalmingPlugin()
		{
			Logger = base.Logger;

			Logger.Log(LogLevel.Debug, "SmartPalming pre");

			_enableSmartPalming = Config.Bind("General", "Enable Smart Palming", true,
				"Enables smart palming. Smart Palming occurs when you duplicate palmed rounds with a mag/gun in your other hand, where it will only take as many rounds as needed out of the palm.");
			_addPlusOneForChamber = Config.Bind("General", "Add +1 for Chamber", false,
				"If the chamber on the gun is empty or spent, then another round will be added to the palm stack you pull out.");

			Harmony.CreateAndPatchAll(typeof(SmartPalmingPlugin));

			Logger.Log(LogLevel.Debug, "SmartPalming post");
		}

		[HarmonyPatch(typeof(FVRFireArmRound), nameof(FVRFireArmRound.DuplicateFromSpawnLock))]
		[HarmonyPostfix]
		public static void FVRFireArmRound_DuplicateFromSpawnLock(FVRFireArmRound __instance, ref GameObject __result, FVRViveHand hand)
		{
			FVRFireArmRound round = __result.GetComponent<FVRFireArmRound>();
			if (_enableSmartPalming.Value && round != null && hand.OtherHand.CurrentInteractable != null)
			{
				int roundsNeeded = 0;

				FVRFireArmMagazine mag = hand.OtherHand.CurrentInteractable.GetComponentInChildren<FVRFireArmMagazine>();
				if (mag != null)
					roundsNeeded = mag.m_capacity - mag.m_numRounds;

				FVRFireArmClip clip = hand.OtherHand.CurrentInteractable.GetComponentInChildren<FVRFireArmClip>();
				if (clip != null)
					roundsNeeded = clip.m_capacity - clip.m_numRounds;

				if (_addPlusOneForChamber.Value && hand.OtherHand.CurrentInteractable is FVRFireArm)
				{
					FVRFireArmChamber[] chambers = FirearmAPI.GetFirearmChambers(hand.OtherHand.CurrentInteractable as FVRFireArm);

					for (int i = 0; i < chambers.Length; i++)
						if (chambers[i].IsManuallyChamberable && (!chambers[i].IsFull || chambers[i].IsSpent))
							roundsNeeded += 1;
				}

				//if rounds are needed, and if rounds needed is less than the proxy rounds + the real round (1)
				if (roundsNeeded > 0 && roundsNeeded < round.ProxyRounds.Count+1)
				{
					for (int i = roundsNeeded-1; i < round.ProxyRounds.Count; i++)
						Destroy(round.ProxyRounds[i].GO);
					round.ProxyRounds.RemoveRange(roundsNeeded-1, (round.ProxyRounds.Count+1) - roundsNeeded);
					round.UpdateProxyDisplay();
				}
			}
		}
	}
}
