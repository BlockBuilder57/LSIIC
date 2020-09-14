using Anvil;
using FistVR;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Valve.VR;

namespace LSIIC.Core
{
	public class HarmonyPatches
	{
		/*
		 * Misc Patches
		 * Mostly changes that make things less annoying
		 */
		[HarmonyPatch(typeof(FVRFireArmMagazine), "Awake")]
		[HarmonyPatch(typeof(FVRFireArmClip), "Awake")]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> IncreaseLoadedRoundsArrayLengthForMagazinesAndClips(IEnumerable<CodeInstruction> instrs)
		{
			return new CodeMatcher(instrs).MatchForward(false,
				new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "m_capacity"),
				new CodeMatch(i => i.opcode == OpCodes.Newarr),
				new CodeMatch(i => i.opcode == OpCodes.Stfld && ((FieldInfo)i.operand).Name == "LoadedRounds"))
			.Repeat(m =>
			{
				m.Advance(1)
				.InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4, 0x400))
				.InsertAndAdvance(new CodeInstruction(OpCodes.Mul, null));
			})
			.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(SteamVR_Action_In_Source), "UpdateOriginTrackedDeviceInfo")]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> RemoveAnnoyingLogSpam(IEnumerable<CodeInstruction> instrs)
		{
			return new CodeMatcher(instrs).MatchForward(false,
				new CodeMatch(i => i.opcode == OpCodes.Ldloc_0),
				new CodeMatch(i => i.opcode == OpCodes.Brfalse))
			.Repeat(m =>
			{
				m.SetOpcodeAndAdvance(OpCodes.Ldc_I4_0);
			})
			.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(FVRPivotLocker), "FVRUpdate")]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> PivotLockerObjectParentageCheck(IEnumerable<CodeInstruction> instrs)
		{
			return new CodeMatcher(instrs).MatchForward(false,
				new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "get_parent"),
				new CodeMatch(i => i.opcode == OpCodes.Ldnull),
				new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "op_Inequality"))
			.Repeat(m =>
			{
				m.Advance(1)
				.SetOpcodeAndAdvance(OpCodes.Ldarg_0)
				.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Component), "transform")));
			})
			.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(FVRPivotLocker), "FVRUpdate")]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> PivotLockerObjectParentageSet(IEnumerable<CodeInstruction> instrs)
		{
			return new CodeMatcher(instrs).MatchForward(false,
				new CodeMatch(i => i.opcode == OpCodes.Ldnull),
				new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "SetParent"))
			.Repeat(m =>
			{
				m.SetOpcodeAndAdvance(OpCodes.Ldarg_0)
				.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Component), "transform")));
			})
			.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(FVRPivotLocker), "IsInteractable")]
		[HarmonyPrefix]
		public static bool PivotLockerUnlockParentage(FVRPivotLocker __instance, ref bool __result)
		{
			__result = !__instance.IsPickUpLocked;
			return false;
		}

		[HarmonyPatch(typeof(FVRPivotLocker), "UnlockObject")]
		[HarmonyPrefix]
		public static bool PivotLockerUnlockParentage(FVRPivotLocker __instance, FVRPhysicalObject ___m_obj)
		{
			___m_obj.transform.SetParent(null);
			return true;
		}

		/*
		 * Functionality patches
		 * The fun stuff
		 */
		[HarmonyPatch(typeof(FVRMovementManager), "Jump")]
		[HarmonyPrefix]
		public static bool StopTimeOnJump(FVRMovementManager __instance)
		{
			if (CorePlugin._timeStopsOnJump.Value)
			{
				Time.timeScale = Time.timeScale == 0 ? 1 : 0;
				Time.fixedDeltaTime = Time.timeScale / SteamVR.instance.hmd_DisplayFrequency;
			}
			return !CorePlugin._timeStopsOnJump.Value; //skip the rest of the function when the config is true
		}

		[HarmonyPatch(typeof(FVRWristMenu), nameof(FVRWristMenu.Awake))]
		[HarmonyPostfix]
		public static void OverflowClockText(FVRWristMenu __instance)
		{
			__instance.SetSelectedButton(0);
			__instance.Clock.verticalOverflow = VerticalWrapMode.Overflow;
			__instance.Clock.horizontalOverflow = HorizontalWrapMode.Overflow;
		}

		[HarmonyPatch(typeof(FVRWristMenu), nameof(FVRWristMenu.Update))]
		[HarmonyPostfix]
		public static void UpdateTimeScaleText(FVRWristMenu __instance, bool ___m_isActive, bool ___m_hasHands, FVRViveHand ___m_currentHand)
		{
			if (___m_isActive)
			{
				if (__instance.Clock.alignment != TextAnchor.LowerCenter)
				{
					__instance.Clock.rectTransform.anchoredPosition = new Vector2(0f, 7.8f);
					__instance.Clock.alignment = TextAnchor.LowerCenter;
				}

				Helpers.H3Info info = Helpers.H3Info.FPS | Helpers.H3Info.DateTime | Helpers.H3Info.Position | Helpers.H3Info.Health | Helpers.H3Info.Scene;
				if (___m_hasHands)
					info |= ___m_currentHand.IsThisTheRightHand ? Helpers.H3Info.ControllerR : Helpers.H3Info.ControllerL;
				__instance.Clock.text = Helpers.H3InfoPrint(info) + "\nH3 Enhanced Wrist Menu";
			}
		}

		[HarmonyPatch(typeof(MainMenuScreen), "Awake")]
		[HarmonyPrefix]
		public static bool AddUnusedPostsToMainMenu(MainMenuScreen __instance)
		{
			foreach (UnityEngine.Transform child in UnityEngine.GameObject.Find("MainMenuSceneProto1").transform)
			{
				if (child.gameObject.name.StartsWith("Post"))
					child.gameObject.SetActive(true);
			}

			return true;
		}

		[HarmonyPatch(typeof(FVRViveHand), nameof(FVRViveHand.GetThrowLinearVelWorld))]
		[HarmonyPatch(typeof(FVRViveHand), nameof(FVRViveHand.GetThrowAngularVelWorld))]
		[HarmonyPostfix]
		public static void AccountForPlayerScaleInBothVelWorld(FVRViveHand __instance, ref Vector3 __result)
		{
			if (GM.CurrentPlayerRoot.localScale != Vector3.one)
				__result = new Vector3(__result.x * GM.CurrentPlayerRoot.localScale.x, __result.y * GM.CurrentPlayerRoot.localScale.y, __result.z * GM.CurrentPlayerRoot.localScale.z);
		}
	}
}
