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
using Valve.VR;

namespace LSIIC.Core
{
	public class Helpers
	{
		public static string SceneName;
		public static int SceneIndex;

		public static string H3InfoPrint(H3Info options, bool controllerDirection = true)
		{
			string ret = "";
			//0b00101111 

			if (options.HasFlag(H3Info.FPS))
				ret += $"\n{Time.timeScale / Time.smoothDeltaTime:F0} FPS ({(1f / Time.timeScale) * Time.deltaTime * 1000:F2}ms) ({Time.timeScale}x)";
			if (options.HasFlag(H3Info.DateTime))
				ret += "\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); //ISO 8601 best girl
			if (options.HasFlag(H3Info.Transform))
				ret += $"\nTransform: {GM.CurrentPlayerRoot.transform.position:F3}@{Mathf.RoundToInt(GM.CurrentPlayerRoot.eulerAngles.y)}°";
			if (options.HasFlag(H3Info.Health))
				ret += $"\nHealth: {GM.CurrentPlayerBody.GetPlayerHealthRaw()}/{GM.CurrentPlayerBody.GetMaxHealthPlayerRaw()} ({(GM.CurrentPlayerBody.GetPlayerHealth() * 100):F0}%)";
			if (options.HasFlag(H3Info.Scene))
				ret += $"\nScene: {SceneName} - level{SceneIndex}";
			if (options.HasFlag(H3Info.SAUCE))
				ret += $"\n{GM.Omni.OmniUnlocks.SaucePackets} S.A.U.C.E.";
			if (options.HasFlag(H3Info.Headset))
				ret += $"\nHeadset: {SteamVR.instance.hmd_ModelNumber}";
			if (options.HasFlag(H3Info.ControllerL))
			{
				FVRViveHand left = GM.CurrentPlayerBody.LeftHand.GetComponent<FVRViveHand>();
				ret += $"\n{(controllerDirection ? " Left " : "")}Controller: {H3InfoPrint_Controllers(left.Pose[left.HandSource].trackedDeviceIndex)}";
			}
			if (options.HasFlag(H3Info.ControllerR))
			{
				FVRViveHand right = GM.CurrentPlayerBody.RightHand.GetComponent<FVRViveHand>();
				ret += $"\n{(controllerDirection ? "Right " : "")}Controller: {H3InfoPrint_Controllers(right.Pose[right.HandSource].trackedDeviceIndex)}";
			}

			if (ret[0] == '\n')
				ret = ret.Substring(1);

			return ret;
		}

		private static int m_maxControllerStringSize;

		public static string H3InfoPrint_Controllers(uint index)
		{
			string info = GetStringTrackedDeviceProperty(index, ETrackedDeviceProperty.Prop_ModelNumber_String);
			if (GetBoolTrackedDeviceProperty(index, ETrackedDeviceProperty.Prop_DeviceProvidesBatteryStatus_Bool))
			{
				bool charging = GetBoolTrackedDeviceProperty(index, ETrackedDeviceProperty.Prop_DeviceIsCharging_Bool);
				info += $" ({GetFloatTrackedDeviceProperty(index, ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float) * 100f:F0}%{(charging ? " +" : "")})";
			}

			if (info.Length > m_maxControllerStringSize)
				m_maxControllerStringSize = info.Length;

			return info;
		}

		[Flags]
		public enum H3Info
		{
			FPS = 0x1,
			DateTime = 0x2,
			Transform = 0x4,
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
		 * These three are only needed because of weird behaviour in BepInEx when it comes to keybinds
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

		/*
		 * SteamVR doesn't expose some property things in its instance for some reason, so these are a nice easy way to access them all
		 */
		private static ETrackedPropertyError m_errorDummy;

		public static bool GetBoolTrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop)
			=> SteamVR.instance.hmd.GetBoolTrackedDeviceProperty(unDeviceIndex, prop, ref m_errorDummy);
		public static float GetFloatTrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop)
			=> SteamVR.instance.hmd.GetFloatTrackedDeviceProperty(unDeviceIndex, prop, ref m_errorDummy);
		public static int GetInt32TrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop)
			=> SteamVR.instance.hmd.GetInt32TrackedDeviceProperty(unDeviceIndex, prop, ref m_errorDummy);
		public static ulong GetUint64TrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop)
			=> SteamVR.instance.hmd.GetUint64TrackedDeviceProperty(unDeviceIndex, prop, ref m_errorDummy);
		public static string GetStringTrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop)
			=> SteamVR.instance.GetStringProperty(prop, unDeviceIndex);
	}
}