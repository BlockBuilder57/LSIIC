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
		public FVRInteractiveObject Object
		{
			get { return m_object; }
			set
			{
				m_prevObject = m_object;
				m_object = value;
			}
		}

		[Header("Held Object Page")]
		public Vector2[] Columns = new Vector2[] { new Vector2(20, -16), new Vector2(130, -16), new Vector2(240, -16) };
		public bool ClearObjectControlsOnRelease = true;

		private FVRInteractiveObject m_object;
		private FVRInteractiveObject m_prevObject;

		private Type[] m_allowedInteractables = { typeof(FVRFireArmAttachmentInterface) };

		private ModPanelV2ObjectControl m_clearObjectControl;
		private int[] m_columnStarts;

		private FVRFireArmChamber m_curChamber;
		private int m_curChamberIndex = -1;

		public override void PageInit()
		{
			base.PageInit();

			if (m_clearObjectControl == null)
			{
				m_clearObjectControl = AddObjectControl(new Vector2(20, 16), 0, this, "ClearObjectControlsOnRelease", "Clear object controls on release? Currently {3}");
				SavedObjectControls.Add(m_clearObjectControl);
			}
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
			if (ClearObjectControlsOnRelease)
			{
				if (Object != null && Object.m_hand == null)
				{
					Object = null;
					CleanupHeldObject();
				}

				//search for any interactive in the hands
				//also check for a hand->hand transfer, ie mag put into gun, as that case doesn't fire ObjectPickedUpEvent
				foreach (FVRViveHand hand in GM.CurrentMovementManager.Hands)
				{
					if (hand.CurrentInteractable != null)
					{
						if (Object != null && Object != hand.CurrentInteractable)
						{
							//only allow FVRInteractiveObjects in the whitelist to actually overwrite Object
							if (!(hand.CurrentInteractable is FVRPhysicalObject))
							{
								bool notTypeOrSubclass = false;
								for (int i = 0; i < m_allowedInteractables.Length; i++)
								{
									Type ciType = hand.CurrentInteractable.GetType();
									Type allowedType = m_allowedInteractables[i];
									if (!ciType.IsSubclassOf(allowedType) && ciType != allowedType)
										notTypeOrSubclass = true;
								}

								if (notTypeOrSubclass)
									continue;
							}

							if (Object == hand.OtherHand.CurrentInteractable && m_prevObject != hand.CurrentInteractable)
							{
								Object = hand.CurrentInteractable;
								RefreshObjectControls();
								break;
							}
							else if (Object != hand.OtherHand.CurrentInteractable)
							{
								Object = hand.CurrentInteractable;
								RefreshObjectControls();
								break;
							}
						}
						else if (Object == null)
						{
							Object = hand.CurrentInteractable;
							RefreshObjectControls();
							break;
						}
					}
				}
			}
#endif

			if (m_curChamberIndex != -1)
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
			if (Panel != null && Panel.PageNameText != null)
				Panel.PageNameText.text = PageTitle;

			m_curChamber = null;
			m_curChamberIndex = -1;
		}

		public void TryToGetCurrentChamber()
		{
			if (Object != null && Object.GetComponent<FVRFireArm>() != null)
			{
				if (AccessTools.Field(Object.GetType(), "Chamber") != null)
				{
					m_curChamber = (FVRFireArmChamber)AccessTools.Field(Object.GetType(), "Chamber").GetValue(Object);
					m_curChamberIndex = -1;
				}
				else if (AccessTools.Field(Object.GetType(), "m_curChamber") != null)
				{
					int CurChamber = (int)AccessTools.Field(Object.GetType(), "m_curChamber").GetValue(Object);
					if ((CurChamber != m_curChamberIndex || m_curChamber == null) && Object.GetType().GetField("Chambers") != null)
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
		}

		public void RefreshObjectControls(FVRInteractiveObject obj = null)
		{
			CleanupHeldObject();

			if (obj != null)
				Object = obj;

			m_columnStarts = new int[] { 0, 0, 0 };

			if (Object != null)
			{
#if !UNITY_EDITOR && !UNITY_STANDALONE
				if (Panel != null && Panel.PageNameText != null)
				{
					if (Object is FVRInteractiveObject)
					{
						m_columnStarts[0] = AddObjectControls(Columns[0], m_columnStarts[0], Object, new string[] { "ControlType", "UseGrabPointChild", "UseGripRotInterp" });
						Panel.PageNameText.text = PageTitle + " - " + Object.gameObject.name;
					}
					if (Object is FVRPhysicalObject)
					{
						FVRPhysicalObject PhysObject = Object as FVRPhysicalObject;
						if (PhysObject.ObjectWrapper != null)
							Panel.PageNameText.text = PageTitle + " - " + PhysObject.ObjectWrapper.DisplayName;
						m_columnStarts[0] = AddObjectControls(Columns[0], m_columnStarts[0], Object, new string[] { "SpawnLockable", "Harnessable", "Size", "ThrowVelMultiplier", "ThrowAngMultiplier", "UsesGravity", "DistantGrabbable", "DoesQuickbeltSlotFollowHead", "IsPickUpLocked", "m_doesDirectParent" });
						m_columnStarts[0] = AddObjectControls(Columns[0], m_columnStarts[0], Object, new string[] { "ToggleKinematicLocked" }, null, 0, 0b1);
					}
				}

				if (Object.GetComponentInChildren<FVRFireArmMagazine>() != null)
					m_columnStarts[2] = AddObjectControls(Columns[2], m_columnStarts[2], Object.GetComponentInChildren<FVRFireArmMagazine>(), new string[] { "m_capacity", "IsInfinite", "MagazineType", "RoundType", "FuelAmountLeft", "CanManuallyEjectRounds" });
				if (Object.GetComponentInChildren<FVRFireArmClip>() != null)
					m_columnStarts[2] = AddObjectControls(Columns[2], m_columnStarts[2], Object.GetComponentInChildren<FVRFireArmClip>(), new string[] { "m_capacity", "IsInfinite", "ClipType", "RoundType", "CanManuallyEjectRounds" });

				if (Object is FVRPhysicalObject)
				{
					if (Object is FVRFireArm)
					{
						AddObjectControl(Columns[0], 15, Object as FVRFireArm, "RoundType"); //kept around for auto zeroing

						TryToGetCurrentChamber();
						if (m_curChamber != null)
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], m_curChamber, new string[] { "ChamberVelocityMultiplier", "IsManuallyExtractable" });

						if (Object is Handgun)
						{
							Handgun handgun = Object as Handgun;
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, handgun, new string[] { "HasSlideRelease", "HasSlideReleaseControl", "HasSlideLockFunctionality", "HasManualDecocker", "HasMagReleaseInput", "CanPhysicsSlideRack" });
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, handgun.Slide, new string[] { "Speed_Forward", "Speed_Rearward", "Speed_Held", "SpringStiffness", "HasLastRoundSlideHoldOpen" });
							AddObjectControls(Columns[2], 14, handgun.FireSelectorModes[handgun.FireSelectorModeIndex], new string[] { "ModeType", "BurstAmount" }, null, 0b1);
						}

						else if (Object is OpenBoltReceiver)
						{
							OpenBoltReceiver obr = Object as OpenBoltReceiver;
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, obr, new string[] { "HasMagReleaseButton" });
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, obr.Bolt, new string[] { "BoltSpeed_Forward", "BoltSpeed_Rearward", "BoltSpeed_Held", "BoltSpringStiffness", "HasLastRoundBoltHoldOpen", "BoltRot_Standard", "BoltRot_Safe", "BoltRot_SlipDistance" });
							AddObjectControls(Columns[2], 14, obr.FireSelector_Modes[obr.FireSelectorModeIndex], new string[] { "ModeType" }, null, 0b1);
							AddObjectControls(Columns[2], 15, obr, new string[] { "SuperBurstAmount" });
						}

						else if (Object is ClosedBoltWeapon)
						{
							ClosedBoltWeapon cbw = Object as ClosedBoltWeapon;
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, cbw, new string[] { "EjectsMagazineOnEmpty", "BoltLocksWhenNoMagazineFound", "DoesClipEntryRequireBoltBack", "HasMagReleaseButton", "HasBoltReleaseButton" });
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, cbw.Bolt, new string[] { "Speed_Forward", "Speed_Rearward", "Speed_Held", "SpringStiffness", "HasLastRoundBoltHoldOpen", "DoesClipHoldBoltOpen" });
							AddObjectControls(Columns[2], 14, cbw.FireSelector_Modes[cbw.FireSelectorModeIndex], new string[] { "ModeType", "BurstAmount" }, null, 0b1);
						}

						else if (Object is BreakActionWeapon)
						{
							BreakActionWeapon baw = Object as BreakActionWeapon;
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], baw, new string[] { "m_isLatched", "UsesManuallyCockedHammers", "FireAllBarrels", "PopOutEmpties" }, null, 0, 0b1000);
							for (int i = 0; i < Math.Min(baw.Barrels.Length, 14); i++) //capped to 14 to avoid controls overflowing
								m_columnStarts[2] = AddObjectControls(Columns[2], m_columnStarts[2], baw.Barrels[i].Chamber, new string[] { "ChamberVelocityMultiplier" });
						}

						else if (Object is Derringer)
						{
							Derringer derringer = Object as Derringer;
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], derringer, new string[] { "HingeValues", "DoesAutoEjectRounds", "IsTriggerDoubleAction", "DeletesCartridgeOnFire" });
							for (int i = 0; i < Math.Min(derringer.Barrels.Count, 14); i++) //capped to 14 to avoid controls overflowing
								m_columnStarts[2] = AddObjectControls(Columns[2], m_columnStarts[2], derringer.Barrels[i].Chamber, new string[] { "ChamberVelocityMultiplier" });
						}

						else if (Object is TubeFedShotgun)
						{
							TubeFedShotgun tfs = Object as TubeFedShotgun;
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, tfs, new string[] { "m_isHammerCocked", "UsesSlamFireTrigger", "CanModeSwitch" });
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, tfs.Bolt, new string[] { "Speed_Forward", "Speed_Rearward", "Speed_Held", "SpringStiffness", "HasLastRoundBoltHoldOpen" });
							if (tfs.Handle != null)
								m_columnStarts[2] = AddObjectControls(Columns[2], m_columnStarts[2] + 1, tfs.Handle, new string[] { "Speed_Held", "m_isHandleLocked" });
						}

						else if (Object is Flaregun)
						{
							Flaregun flaregun = Object as Flaregun;
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, flaregun, new string[] { "HingeAxis", "RotOut", "CanUnlatch", "IsHighPressureTolerant", "m_isHammerCocked", "m_isDestroyed", "CocksOnOpen" });
						}

						else if (Object is SimpleLauncher)
						{
							SimpleLauncher sl = Object as SimpleLauncher;
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, sl, new string[] { "HasTrigger", "AlsoPlaysSuppressedSound", "DeletesCartridgeOnFire", "FireOnCol", "ColThresh" });
						}

						else if (Object is BoltActionRifle)
						{
							BoltActionRifle bar = Object as BoltActionRifle;
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, bar, new string[] { "HasMagEjectionButton", "m_isHammerCocked", "EjectsMagazineOnEmpty", "HasMagEjectionButton" });
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, bar.BoltHandle, new string[] { "UsesQuickRelease", "BaseRotOffset", "MinRot", "MaxRot", "UnlockThreshold" });
						}

						else if (Object is Revolver)
						{
							Revolver revolver = Object as Revolver;
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, revolver, new string[] { "AllowsSuppressor", "CanManuallyCockHammer", "m_isHammerLocked", "m_isCylinderArmLocked", "CylinderRotRange", "IsCylinderArmZ", "GravityRotsCylinderPositive" });
						}

						else if (Object is SingleActionRevolver)
						{
							SingleActionRevolver saRevolver = Object as SingleActionRevolver;
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, saRevolver, new string[] { "AllowsSuppressor", "DoesCylinderTranslateForward", "DoesHalfCockHalfRotCylinder", "HasTransferBarSafety" });
						}

						else if (Object is LAPD2019)
						{
							LAPD2019 lapd = Object as LAPD2019;
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, lapd, new string[] { "m_isCylinderArmLocked", "CylinderRotRange", "GravityRotsCylinderPositive", "m_isAutoChargeEnabled", "m_hasBattery", "m_batteryCharge", "m_hasThermalClip", "m_heatThermalClip", "m_heatSystem", "m_barrelHeatDamage" });
							m_columnStarts[2] = AddObjectControls(Columns[2], m_columnStarts[2], lapd.BoltHandle, new string[] { "UsesQuickRelease", "BaseRotOffset", "MinRot", "MaxRot", "UnlockThreshold" });
						}

						else if (Object is RevolvingShotgun)
						{
							RevolvingShotgun revolvingShotgun = Object as RevolvingShotgun;
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, revolvingShotgun, new string[] { "DoesFiringRecock", "CylinderLoaded", "IsCylinderRotClockwise", "EjectCylinder" }, null, 0, 0b1000);
							AddObjectControls(Columns[2], 15, revolvingShotgun.FireSelector_Modes[revolvingShotgun.FireSelectorModeIndex], new string[] { "ModeType" }, null, 0b1);
						}
					}

					else if (Object is FVRFireArmRound)
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], Object as FVRFireArmRound, new string[] { "RoundType", "RoundClass", "IsHighPressure", "NumProjectiles", "ProjectileSpread", "IsDestroyedAfterCounter", "m_isKillCounting", "isCookingOff", "isManuallyChamberable", "IsCaseless", "isMagazineLoadable", "isPalmable", "MaxPalmedAmount" });

					else if (Object is PinnedGrenade)
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], Object as FVRGrenade, new string[] { "ReleaseLever", "DefaultFuse", "HasImpactFuse" }, null, 0, 0b1);

					else if (Object is MF2_Medigun)
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], Object as MF2_Medigun, new string[] { "EngageRange", "MaxRange", "TryEngageBeam", "EngageUber", "DisEngageBeam", "m_uberChargeUp", "m_uberChargeOut" }, null, 0b1100000, 0b11100);

					else if (Object.GetComponent<SosigLink>() != null)
					{
						SosigLink L = Object.GetComponent<SosigLink>();
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], L.S, new string[] { "ClearSosig", "", "BodyState", "CurrentOrder", "Mustard", "BleedDamageMult", "BleedRateMult", "Speed_Crawl", "Speed_Sneak", "Speed_Walk", "Speed_Run" }, null, 0b11100, 0b1);
						m_columnStarts[2] = AddObjectControls(Columns[2], m_columnStarts[2], L, new string[] { "m_integrity", "StaggerMagnitude", "DamMult" }, null, 0b1);
						m_columnStarts[2] = AddObjectControls(Columns[2], m_columnStarts[2] + 1, L.S.E, new string[] { "IFFCode" });
					}

					else if (Object is SosigWeaponPlayerInterface)
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], (Object as SosigWeaponPlayerInterface).W, new string[] { "CycleSpeedForward", "CycleSpeedBackward", "ShotsPerLoad", "m_shotsLeft", "ProjectilesPerShot", "ProjectileSpread", "isFullAuto", "ReloadTime", "BurstLimit" });

					else if (Object is RW_Powerup)
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], Object as RW_Powerup, new string[] { "PowerupType", "PowerupIntensity", "PowerupDuration", "Cooked", "UpdateSymbols" }, null, 0, 0b10000);

					else if (Object is ShatterablePhysicalObject)
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], Object as ShatterablePhysicalObject, new string[] { "currentToughness", "TransfersVelocityExplosively", "DamageReceivedMultiplier", "CollisionShatterThreshold" });

					else if (Object.GetComponent<RotrwBangerJunk>() != null)
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], Object.GetComponent<RotrwBangerJunk>(), new string[] { "Type", "ContainerSize" });

					else if (Object is Banger)
					{
						Banger banger = Object as Banger;
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], banger, new string[] { "BType", "BSize", "m_isArmed", "m_timeToPayload", "ProxRange", "m_timeSinceArmed", "m_shrapnelVel", "m_isSticky", "m_isSilent", "m_isHoming", "m_canbeshot", "SetToBouncy" }, null, 0, 0b100000000000);

						if (banger.BDial != null)
							m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], banger.BDial, new string[] { "DialTick", "m_isPrimed", "m_hasDinged" });
					}

					else if (Object is BangerDetonator)
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], Object as BangerDetonator, new string[] { "Detonate", "TriggerRange" }, null, 0, 0b1);

					else if (Object is GronchHatCase)
						m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], Object as GronchHatCase, new string[] { "m_isOpen", "HID" });

					else if (Object.GetComponent<FVRFireArmAttachment>() != null && Object.GetComponent<FVRFireArmAttachment>().AttachmentInterface != null)
						RefreshObjectControls_AttachmentInterfaces(Object.GetComponent<FVRFireArmAttachment>().AttachmentInterface);
				}
				else if (Object is FVRInteractiveObject)
				{
					if (Object is FVRFireArmAttachmentInterface)
						RefreshObjectControls_AttachmentInterfaces(Object as FVRFireArmAttachmentInterface);
				}
#endif
			}
		}

		public void RefreshObjectControls_AttachmentInterfaces(FVRFireArmAttachmentInterface attachInterface)
		{
#if !UNITY_EDITOR && !UNITY_STANDALONE
			if (attachInterface is Amplifier && (attachInterface as Amplifier).ScopeCam != null)
				m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], (attachInterface as Amplifier).ScopeCam, new string[] { "OnEnable", "Magnification", "Resolution", "AngleBlurStrength", "CutoffSoftness", "AngularOccludeSensitivity", "ReticuleScale", "MagnificationEnabledAtStart", "LensSpaceDistortion", "LensChromaticDistortion" }, null, 0, 0b1);
#endif
		}
	}
}
