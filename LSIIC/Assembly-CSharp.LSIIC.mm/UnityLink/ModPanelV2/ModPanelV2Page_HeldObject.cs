using FistVR;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LSIIC.ModPanel
{
	public class ModPanelV2Page_HeldObject : ModPanelV2Page
	{
		public FVRPhysicalObject Object;
		public Vector2[] Columns = new Vector2[] { new Vector2(20, -16), new Vector2(130, -16), new Vector2(240, -16) };
		public bool ClearObjectControlsOnRelease = true;

		private ModPanelV2ObjectControl m_clearObjectControl;
		private int[] m_columnStarts;

		private FVRFireArmChamber m_curChamber;
		private int m_curChamberIndex;

		public override void PageInit()
		{
			base.PageInit();

			if (m_clearObjectControl == null)
				m_clearObjectControl = AddObjectControl(new Vector2(20, 16), 0, this, "ClearObjectControlsOnRelease", "Clear object controls on release? Currently {3}");
		}

		public override void PageOpen()
		{
			base.PageOpen();
			GM.CurrentSceneSettings.ObjectPickedUpEvent += RefreshObjectControls;
		}

		public override void PageTick()
		{
			base.PageTick();

#if UNITY_EDITOR
			if (Object != null && ObjectControls.Count <= 1)
			{
				RefreshObjectControls(Object);
			}
#else
			if (ClearObjectControlsOnRelease && Object != null && Object.m_hand == null)
			{
				CleanupHeldObject();

				//check for a hand->hand transfer, ie mag put into gun, as this doesn't fire ObjectPickedUpEvent
				foreach (FVRViveHand hand in GM.CurrentMovementManager.Hands)
				{
					if (hand.CurrentInteractable != null && hand.CurrentInteractable is FVRPhysicalObject)
					{
						FVRPhysicalObject obj = hand.CurrentInteractable as FVRPhysicalObject;
						RefreshObjectControls(obj);
						break;
					}
				}
			}
#endif

			TryToGetCurrentChamber();
		}

		public override void PageClose(bool destroy)
		{
			base.PageClose();
			GM.CurrentSceneSettings.ObjectPickedUpEvent -= RefreshObjectControls;
		}

		public void CleanupHeldObject()
		{
			ClearObjectControls();
			Object = null;
			if (Panel != null && Panel.PageNameText != null)
				Panel.PageNameText.text = PageTitle;

			m_curChamber = null;
			m_curChamberIndex = -1;
		}

		public void TryToGetCurrentChamber()
		{
			if (Object != null && Object.GetComponent<FVRFireArm>() != null && AccessTools.Field(Object.GetType(), "m_curChamber") != null)
			{
				int CurChamber = (int)AccessTools.Field(Object.GetType(), "m_curChamber").GetValue(Object);
				if (CurChamber != m_curChamberIndex && Object.GetType().GetField("Chambers") != null)
				{
					m_curChamberIndex = CurChamber;
					FVRFireArmChamber[] Chambers = (FVRFireArmChamber[])Object.GetType().GetField("Chambers").GetValue(Object);
					m_curChamber = Chambers[m_curChamberIndex];

					//i'm so sorry
					foreach (ModPanelV2ObjectControl control in UpdatingObjectControls)
						if (control.Instance.GetType() == typeof(FVRFireArmChamber))
							control.Instance = m_curChamber;
				}
			}
		}

		public void RefreshObjectControls(FVRPhysicalObject obj)
		{
			if (Object != null)
				CleanupHeldObject();

			Object = obj;

			m_columnStarts = new int[] { 0, 0, 0 };

			if (Object != null)
			{
				if (Panel != null && Panel.PageNameText != null && Object.ObjectWrapper != null)
					Panel.PageNameText.text = PageTitle + " - " + Object.ObjectWrapper.DisplayName;

				m_columnStarts[0] = AddObjectControls(Columns[0], m_columnStarts[0], Object, new string[] { "SpawnLockable", "Harnessable", "Size", "QBSlotType", "ThrowVelMultiplier", "ThrowAngMultiplier", "UsesGravity", "DoesQuickbeltSlotFollowHead", "DistantGrabbable", "IsPickUpLocked", "UseGripRotInterp" });
				m_columnStarts[0] = AddObjectControls(Columns[0], m_columnStarts[0], Object, new string[] { "ToggleKinematicLocked" }, null, null, new bool[] { true });

				if (Object.GetComponentInChildren<FVRFireArmMagazine>() != null)
					m_columnStarts[2] = AddObjectControls(Columns[2], m_columnStarts[2], Object.GetComponentInChildren<FVRFireArmMagazine>(), new string[] { "m_capacity", "IsInfinite", "MagazineType", "RoundType", "FuelAmountLeft", "CanManuallyEjectRounds" });
				if (Object.GetComponentInChildren<FVRFireArmClip>() != null)
					m_columnStarts[2] = AddObjectControls(Columns[2], m_columnStarts[2], Object.GetComponentInChildren<FVRFireArmClip>(), new string[] { "m_capacity", "IsInfinite", "ClipType", "RoundType", "CanManuallyEjectRounds" });

#if !UNITY_EDITOR && !UNITY_STANDALONE
				if (Object.GetComponent<FVRFireArm>() != null)
				{
					m_columnStarts[0] = AddObjectControls(Columns[0], m_columnStarts[0] + 1, Object.GetComponent<FVRFireArm>(), new string[] { "MagazineType", "ClipType", "RoundType" });
					if (m_curChamber == null)
						TryToGetCurrentChamber();

					if (AccessTools.Field(Object.GetType(), "Chamber") != null)
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], Object.GetType().GetField("Chamber").GetValue(Object), new string[] { "ChamberVelocityMultiplier", "RoundType", "SpreadRangeModifier" });
					else if (AccessTools.Field(Object.GetType(), "m_curChamber") != null)
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], m_curChamber, new string[] { "ChamberVelocityMultiplier", "RoundType", "SpreadRangeModifier" }, null, new bool[] { true, true, true });

					if (Object.GetComponent<Handgun>() != null)
					{
						Handgun handgun = Object.GetComponent<Handgun>();
						//m_heldTouchpadAction
						AddObjectControls(Columns[1], m_columnStarts[1] + 1, handgun, new string[] { "HasManualDecocker", "HasMagReleaseInput", "CanPhysicsSlideRack" });
						AddObjectControls(Columns[1], m_columnStarts[1] + 1, handgun.Slide, new string[] { "Speed_Forward", "Speed_Rearward", "Speed_Held", "SpringStiffness", "HasLastRoundSlideHoldOpen" });
						AddObjectControls(Columns[2], 14, handgun.FireSelectorModes[handgun.FireSelectorModeIndex], new string[] { "ModeType", "BurstAmount" });
					}

					else if (Object.GetComponent<OpenBoltReceiver>() != null)
					{
						OpenBoltReceiver obr = Object.GetComponent<OpenBoltReceiver>();
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], obr.Bolt, new string[] { "BoltSpeed_Forward", "BoltSpeed_Rearward", "BoltSpeed_Held", "BoltSpringStiffness", "HasLastRoundBoltHoldOpen", "HasMagReleaseButton", "BoltRot_Standard", "BoltRot_Safe", "BoltRot_SlipDistance" });
						AddObjectControls(Columns[2], 14, obr.FireSelector_Modes[obr.FireSelectorModeIndex], new string[] { "ModeType" });
						AddObjectControls(Columns[2], 15, obr, new string[] { "SuperBurstAmount" });
					}

					else if (Object.GetComponent<ClosedBoltWeapon>() != null)
					{
						ClosedBoltWeapon cbw = Object.GetComponent<ClosedBoltWeapon>();
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, cbw, new string[] { "EjectsMagazineOnEmpty", "BoltLocksWhenNoMagazineFound", "DoesClipEntryRequireBoltBack" });
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, cbw.Bolt, new string[] { "Speed_Forward", "Speed_Rearward", "Speed_Held", "SpringStiffness", "HasLastRoundBoltHoldOpen", "HasMagReleaseButton", "UsesAKSafetyLock", "DoesClipHoldBoltOpen" });
						AddObjectControls(Columns[2], 14, cbw.FireSelector_Modes[cbw.FireSelectorModeIndex], new string[] { "ModeType", "BurstAmount" });
					}

					else if (Object.GetComponent<BreakActionWeapon>() != null)
					{
						BreakActionWeapon baw = Object.GetComponent<BreakActionWeapon>();
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], baw, new string[] { "m_isLatched", "UsesManuallyCockedHammers", "FireAllBarrels", "PopOutEmpties" });
						for (int i = 0; i < baw.Barrels.Length; i++)
							m_columnStarts[2] = AddObjectControls(Columns[2], m_columnStarts[2], baw.Barrels[i].Chamber, new string[] { "RoundType", "ChamberVelocityMultiplier", "SpreadRangeModifier" });
					}

					else if (Object.GetComponent<TubeFedShotgun>() != null)
					{
						TubeFedShotgun tfs = Object.GetComponent<TubeFedShotgun>();
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, tfs, new string[] { "m_isHammerCocked", "UsesSlamFireTrigger", "CanModeSwitch" });
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, tfs.Bolt, new string[] { "Speed_Forward", "Speed_Rearward", "Speed_Held", "SpringStiffness", "HasLastRoundBoltHoldOpen" });
						if (tfs.Handle != null)
							m_columnStarts[2] = AddObjectControls(Columns[2], m_columnStarts[2] + 1, tfs.Handle, new string[] { "Speed_Held", "m_isHandleLocked" });
					}

					else if (Object.GetComponent<Flaregun>() != null)
					{
						Flaregun flaregun = Object.GetComponent<Flaregun>();
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, flaregun, new string[] { "HingeAxis", "RotOut", "CanUnlatch", "IsHighPressureTolerant", "m_isHammerCocked", "m_isDestroyed", "CocksOnOpen" });
					}

					else if (Object.GetComponent<SimpleLauncher>() != null)
					{
						SimpleLauncher sl = Object.GetComponent<SimpleLauncher>();
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, sl, new string[] { "HasTrigger", "AlsoPlaysSuppressedSound", "DeletesCartridgeOnFire", "FireOnCol", "ColThresh" });
					}

					else if (Object.GetComponent<BoltActionRifle>() != null)
					{
						BoltActionRifle bar = Object.GetComponent<BoltActionRifle>();
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, bar, new string[] { "HasMagEjectionButton", "m_isHammerCocked", "EjectsMagazineOnEmpty", "HasMagEjectionButton" });
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, bar.BoltHandle, new string[] { "UsesQuickRelease", "BaseRotOffset", "MinRot", "MaxRot", "UnlockThreshold" });
					}

					else if (Object.GetComponent<Revolver>() != null)
					{
						Revolver revolver = Object.GetComponent<Revolver>();
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, revolver, new string[] { "CanManuallyCockHammer", "m_isHammerLocked", "m_isCylinderArmLocked", "CylinderRotRange", "IsCylinderArmZ", "GravityRotsCylinderPositive" });
					}

					else if (Object.GetComponent<LAPD2019>() != null)
					{
						LAPD2019 lapd = Object.GetComponent<LAPD2019>();
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, lapd, new string[] { "m_isCylinderArmLocked", "CylinderRotRange", "GravityRotsCylinderPositive", "m_isAutoChargeEnabled", "m_hasBattery", "m_batteryCharge", "m_hasThermalClip", "m_heatThermalClip", "m_heatSystem", "m_barrelHeatDamage" });
						m_columnStarts[2] = AddObjectControls(Columns[2], m_columnStarts[2], lapd.BoltHandle, new string[] { "UsesQuickRelease", "BaseRotOffset", "MinRot", "MaxRot", "UnlockThreshold" });
					}

					m_curChamberIndex = 0;
				}

				else if (Object.GetComponent<FVRFireArmRound>() != null)
					m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], Object.GetComponent<FVRFireArmRound>(), new string[] { "RoundType", "RoundClass", "IsHighPressure", "NumProjectiles", "ProjectileSpread", "IsDestroyedAfterCounter", "m_isKillCounting", "isCookingOff", "isManuallyChamberable", "IsCaseless", "isMagazineLoadable", "isPalmable", "MaxPalmedAmount" });

				else if (Object.GetComponent<FVRGrenade>() != null)
					m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], Object.GetComponent<FVRGrenade>(), new string[] { "DefaultFuse" });

				else if (Object.GetComponent<MF2_Medigun>() != null)
					m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], Object.GetComponent<MF2_Medigun>(), new string[] { "EngageRange", "MaxRange", "TryEngageBeam", "EngageUber", "DisEngageBeam", "m_uberChargeUp", "m_uberChargeOut" });

				else if (Object.GetComponent<SosigLink>() != null)
				{
					SosigLink L = Object.GetComponent<SosigLink>();
					m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], L.S, new string[] { "ClearSosig", "", "BodyState", "CurrentOrder", "Mustard", "BleedDamageMult", "BleedRateMult", "Speed_Crawl", "Speed_Sneak", "Speed_Walk", "Speed_Run" });
					m_columnStarts[2] = AddObjectControls(Columns[2], m_columnStarts[2], L, new string[] { "m_integrity", "StaggerMagnitude", "DamMult" });
				}

				else if (Object.GetComponent<SosigWeaponPlayerInterface>() != null)
					m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], Object.GetComponent<SosigWeaponPlayerInterface>().W, new string[] { "CycleSpeedForward", "CycleSpeedBackward", "ShotsPerLoad", "m_shotsLeft", "ProjectilesPerShot", "ProjectileSpread", "isFullAuto", "ReloadTime", "BurstLimit" });

				else if (Object.GetComponent<RW_Powerup>() != null)
					m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], Object.GetComponent<RW_Powerup>(), new string[] { "PowerupType", "PowerupIntensity", "PowerupDuration", "PowerUpSpecial", "Cooked", "UpdateSymbols" });

				else if (Object.GetComponent<ShatterablePhysicalObject>() != null)
					m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], Object.GetComponent<ShatterablePhysicalObject>(), new string[] { "currentToughness", "TransfersVelocityExplosively", "DamageReceivedMultiplier", "CollisionShatterThreshold" });

				else if (Object.GetComponent<RotrwBangerJunk>() != null)
					m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], Object.GetComponent<RotrwBangerJunk>(), new string[] { "Type", "ContainerSize" });

				else if (Object.GetComponent<Banger>() != null)
				{
					Banger banger = Object.GetComponent<Banger>();
					m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], banger, new string[] { "BType", "BSize", "m_isArmed", "m_timeToPayload", "ProxRange", "m_timeSinceArmed", "m_shrapnelVel", "m_isSticky", "m_isSilent", "m_isHoming", "m_canbeshot", "SetToBouncy"});

					if (banger.BDial != null)
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], banger.BDial, new string[] { "DialTick", "m_isPrimed", "m_hasDinged" });
				}

				else if (Object.GetComponent<BangerDetonator>() != null)
					m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], Object.GetComponent<BangerDetonator>(), new string[] { "TriggerRange", "Detonate" });

				else if (Object.GetComponent<GronchHatCase>() != null)
					m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], Object.GetComponent<GronchHatCase>(), new string[] { "m_isOpen", "Open", "HID" }, null, null, new bool[] { false, true, false }, new object[][] { null, new object[] { new GameObject("dummy").AddComponent<GronchHatCaseKey>() }, null });
#endif
			}
		}
	}
}
