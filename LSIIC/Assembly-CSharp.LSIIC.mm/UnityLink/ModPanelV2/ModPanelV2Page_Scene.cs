using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LSIIC.ModPanel
{
	public class ModPanelV2Page_Scene : ModPanelV2Page
	{
		[Header("Scene Page")]
		public Vector2[] Columns = new Vector2[] { new Vector2(20, -16), new Vector2(130, -16), new Vector2(240, -16) };
		private int[] m_columnStarts = new int[] { 0, 0, 0 };

#pragma warning disable CS0414 //shut your up Unity
		private int m_playerIFF = -3;

		private PowerupType m_powerupType = PowerupType.InfiniteAmmo;
		private PowerUpIntensity m_powerupIntensity = PowerUpIntensity.High;
		private PowerUpDuration m_powerupDuration = PowerUpDuration.SuperLong;
		private bool m_powerupInverted = false;
		private float m_powerupDurationOverride = -1f;

		private FVRSoundEnvironment m_soundEnv = FVRSoundEnvironment.Forest;
#pragma warning restore CS0414

		public override void PageInit()
		{
			base.PageInit();

			if (GM.CurrentSceneSettings != null)
				m_soundEnv = GM.CurrentSceneSettings.DefaultSoundEnvironment;
		}

		public override void PageOpen()
		{
			base.PageOpen();

			if (ObjectControls.Count <= 0)
			{
				if (GM.CurrentSceneSettings != null)
				{
					m_columnStarts[0] = AddObjectControls(Columns[0], m_columnStarts[0], GM.CurrentSceneSettings, new string[] { "IsSpawnLockingEnabled", "DoesDamageGetRegistered", "MaxProjectileRange", "ForcesCasingDespawn", "DoesTeleportUseCooldown", "DoesAllowAirControl", "UsesPlayerCatcher", "CatchHeight", "DefaultPlayerIFF", "IsQuickbeltSwappingAllowed", "IsSceneLowLight", "IsAmmoInfinite", "AllowsInfiniteAmmoMags", "UsesUnlockSystem" });
				}
				if (GM.CurrentPlayerBody != null)
				{
					m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], this, new string[] { "m_powerupType", "m_powerupIntensity", "m_powerupDuration", "m_powerupInverted", "m_powerupDurationOverride", "ActivatePower", "", "SetPlayerIFF" }, new string[] { null, null, null, null, null, "{1} {0}.{2}" }, 0, 0b10100000);
					m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], GM.CurrentPlayerBody, new string[] { "m_playerIFF", "Health", "m_startingHealth" }, null, 0b11);
				}
				if (ManagerSingleton<SM>.Instance != null)
				{
					m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1] + 1, this, new string[] { "", "SetReverbEnvironment", "m_soundEnv" }, new string[] { "This function will only work when the player\nis not in a reverb environment. The Indoor\nRange is a good place to test this out." }, 0, 0b10);
				}
				if (GM.Options != null && GM.Options.ControlOptions != null)
				{
					m_columnStarts[2] = AddObjectControls(Columns[2], m_columnStarts[2], this, new string[] { "UpdateSosigPlayerBodyState" }, null, 0, 0b1);
					m_columnStarts[2] = AddObjectControls(Columns[2], m_columnStarts[2], GM.Options.ControlOptions, new string[] { "MBClothing", "CamFOV", "CamSmoothingLinear", "CamSmoothingRotational", "CamLeveling" }, null, 0b11111);
				}
			}

			if (GM.CurrentPlayerBody != null)
				m_playerIFF = GM.CurrentPlayerBody.GetPlayerIFF();
		}

		public override void PageTick()
		{
			base.PageTick();
		}

		public void ActivatePower()
		{
			if (GM.CurrentPlayerBody != null)
				GM.CurrentPlayerBody.ActivatePower(m_powerupType, m_powerupIntensity, m_powerupDuration, false, m_powerupInverted, m_powerupDurationOverride);
		}

		public void SetPlayerIFF()
		{
			if (GM.CurrentPlayerBody != null)
				GM.CurrentPlayerBody.SetPlayerIFF(m_playerIFF);
		}

		public void SetReverbEnvironment()
		{
			GM.CurrentSceneSettings.DefaultSoundEnvironment = m_soundEnv;
			SM.TransitionToReverbEnvironment(m_soundEnv, 0.1f);
		}

		public void UpdateSosigPlayerBodyState()
		{
			if (GM.Options == null)
				return;

			if (ManagerSingleton<IM>.Instance.odicSosigObjsByID.ContainsKey(GM.Options.ControlOptions.MBClothing))
			{
				SosigEnemyTemplate set = ManagerSingleton<IM>.Instance.odicSosigObjsByID[GM.Options.ControlOptions.MBClothing];
				if (GM.CurrentPlayerBody != null)
					GM.CurrentPlayerBody.SetOutfit(set);
			}
		}
	}
}
