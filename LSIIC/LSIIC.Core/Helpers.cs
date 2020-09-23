using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Alloy;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FistVR;
using HarmonyLib;
using UnityEngine;
using UnityEngine.VR;
using Valve.VR;

namespace LSIIC.Core
{
	public class Helpers
	{
		private static int m_maxControllerStringSize;

		public static string H3InfoPrint(H3Info options)
		{
			string ret = "";
			//0b00101111 

			if (options.HasFlag(H3Info.FPS))
				ret += $"\n{Time.timeScale / Time.smoothDeltaTime:F0} FPS ({(1f / Time.timeScale) * Time.deltaTime * 1000:F2}ms) ({Time.timeScale}x)";
			if (options.HasFlag(H3Info.DateTime))
				ret += "\n" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
			if (options.HasFlag(H3Info.Position))
				ret += $"\nPosition: {GM.CurrentPlayerRoot.position}";
			if (options.HasFlag(H3Info.Health))
				ret += $"\nHealth: {GM.CurrentPlayerBody.GetPlayerHealthRaw()}/{GM.CurrentPlayerBody.GetMaxHealthPlayerRaw()} ({(GM.CurrentPlayerBody.GetPlayerHealth() * 100):F0}%)";
			if (options.HasFlag(H3Info.Scene))
				ret += $"\nScene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} - level{UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex}";
			if (options.HasFlag(H3Info.SAUCE))
				ret += $"\n{GM.Omni.OmniUnlocks.SaucePackets} S.A.U.C.E.";
			if (options.HasFlag(H3Info.Headset))
				ret += $"\nHeadset: {VRDevice.model}";
			if (options.HasFlag(H3Info.ControllerL))
			{
				FVRViveHand left = GM.CurrentPlayerBody.LeftHand.GetComponent<FVRViveHand>();
				ret += $"\n Left Controller: {H3InfoPrint_Controllers(left.Pose[left.HandSource].trackedDeviceIndex)}";
			}
			if (options.HasFlag(H3Info.ControllerR))
			{
				FVRViveHand right = GM.CurrentPlayerBody.RightHand.GetComponent<FVRViveHand>();
				ret += $"\nRight Controller: {H3InfoPrint_Controllers(right.Pose[right.HandSource].trackedDeviceIndex)}";
			}

			if (ret[0] == '\n')
				ret = ret.Substring(1);

			return ret;
		}

		public static string H3InfoPrint_Controllers(uint trackedDeviceIndex)
		{
			string modelNo = SteamVR.instance.GetStringProperty(ETrackedDeviceProperty.Prop_ModelNumber_String, trackedDeviceIndex);
			float batteryPercentage = SteamVR.instance.GetFloatProperty(ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float, trackedDeviceIndex);

			if (modelNo.Length > m_maxControllerStringSize)
				m_maxControllerStringSize = modelNo.Length;

			return $"{modelNo.PadRight(m_maxControllerStringSize)} ({batteryPercentage * 100f:F0}%)";
		}

		[Flags]
		public enum H3Info
		{
			FPS = 0x1,
			DateTime = 0x2,
			Position = 0x4,
			Health = 0x8,
			Scene = 0x10,
			SAUCE = 0x20,
			Headset = 0x40,
			ControllerL = 0x80,
			ControllerR = 0x100,

			All = Int32.MaxValue,
			None = 0
		}

		public static string GetHeldObjects()
		{
			string holdingInfo = "";
			foreach (FVRViveHand hand in GM.CurrentMovementManager.Hands)
			{
				GameObject targetObject = null;
				if (hand.CurrentInteractable != null)
				{
					targetObject = hand.CurrentInteractable.gameObject;
				}
				if (targetObject != null)
				{
					holdingInfo += $"Holding: {targetObject.name}";
					foreach (Component comp in targetObject.GetComponents<Component>())
					{
						Type t = comp.GetType();
						bool firstClass = true;
						while (t?.Namespace != "UnityEngine")
						{
							holdingInfo += firstClass ? "\nType: " + t.ToString() : " : " + t.ToString();
							t = t.BaseType;
							firstClass = false;
						}
					}
					holdingInfo += $"\nLayer(s): {LayerMask.LayerToName(targetObject.layer)}";
					holdingInfo += $"\nTag: {targetObject.tag}";

					if (targetObject.GetComponent<FVRFireArmRound>() != null)
						holdingInfo += GetHeldObjects_RoundInfo(targetObject.GetComponent<FVRFireArmRound>());
					if (targetObject.GetComponentInChildren<FVRFireArmMagazine>() != null)
						holdingInfo += $"\n[Mag Type]: {targetObject.GetComponentInChildren<FVRFireArmMagazine>().RoundType}\n[Mag Rounds]: {targetObject.GetComponentInChildren<FVRFireArmMagazine>().m_numRounds}/{targetObject.GetComponentInChildren<FVRFireArmMagazine>().m_capacity}";
					if (targetObject.GetComponentInChildren<FVRFireArmClip>() != null)
						holdingInfo += $"\n[Clip Type]: {targetObject.GetComponentInChildren<FVRFireArmClip>().RoundType}\n[Clip Rounds]: {targetObject.GetComponentInChildren<FVRFireArmClip>().m_numRounds}/{targetObject.GetComponentInChildren<FVRFireArmClip>().m_capacity}";
					if (targetObject.GetComponent<FVRFireArm>() != null)
						holdingInfo += $"\n[Round Type]: {targetObject.GetComponent<FVRFireArm>().RoundType}";
				}
				if (holdingInfo != "")
					holdingInfo += "\n\n";
			}
			return holdingInfo;
		}

		public static string GetHeldObjects_RoundInfo(FVRFireArmRound round)
		{
			string temp = "";
			temp += $"\n[Round Type]: {round.RoundType}\n[Round Class]: {round.RoundClass}\n[Round Projectile Count:] {round.NumProjectiles}*{round.ProjectileSpread}s";
			temp += $"\n[Round Manually Chamberable]: {round.isManuallyChamberable}\n[Round Loadable]: {round.isMagazineLoadable}";
			temp += $"\n[Round Palmable]: {round.isPalmable}\n[Round Palm Amount]: {round.ProxyRounds.Count + 1}/{round.MaxPalmedAmount + 1}";
			if (round.IsSpent)
				temp += $"\n[Round Status]: Spent\n";
			else
				temp += $"\n[Round Status]: Ready\n";
			return temp;
		}

		/*
		 * These are only needed because of weird behaviour in BepInEx when it comes to keybinds
		 */
		public static bool BepInExGetKeyDown(KeyboardShortcut shortcut)
		{
			if (Input.GetKeyDown(shortcut.MainKey))
			{
				bool allModifiersPressed = shortcut.Modifiers.All(c => Input.GetKey(c));
				return allModifiersPressed;
			}
			return false;
		}

		public static bool BepInExGetKey(KeyboardShortcut shortcut)
		{
			if (Input.GetKey(shortcut.MainKey))
			{
				bool allModifiersPressed = shortcut.Modifiers.All(c => Input.GetKey(c));
				return allModifiersPressed;
			}
			return false;
		}

		public static bool BepInExGetKeyUp(KeyboardShortcut shortcut)
		{
			if (Input.GetKeyUp(shortcut.MainKey))
			{
				bool allModifiersPressed = shortcut.Modifiers.All(c => Input.GetKey(c));
				return allModifiersPressed;
			}
			return false;
		}
	}
}