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

		public override void PageOpen()
		{
			base.PageOpen();

			if (ObjectControls.Count <= 0)
			{
				if (GM.CurrentSceneSettings != null)
					m_columnStarts[0] = AddObjectControls(Columns[0], m_columnStarts[0], GM.CurrentSceneSettings, new string[] { "IsSpawnLockingEnabled", "DoesDamageGetRegistered", "MaxProjectileRange", "DoesTeleportUseCooldown", "DoesAllowAirControl", "UsesPlayerCatcher", "CatchHeight", "DefaultPlayerIFF", "IsQuickbeltSwappingAllowed", "IsSceneLowLight", "IsAmmoInfinite", "AllowsInfiniteAmmoMags", "UsesUnlockSystem" });
				if (GM.CurrentPlayerBody != null)
				{
					m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], this, new string[] { "m_powerupType", "m_powerupIntensity", "m_powerupDuration", "m_powerupInverted", "m_powerupDurationOverride", "ActivatePower", "", "SetPlayerIFF" }, new string[] { null, null, null, null, null, "{1} {0}.{2}" }, null, new bool[] { false, false, false, false, false, true, false, true });
					m_columnStarts[1] = AddObjectControls(Columns[1], m_columnStarts[1], GM.CurrentPlayerBody, new string[] { "m_playerIFF", "Health", "m_startingHealth" });
				}
				if (ManagerSingleton<SM>.Instance != null)
				{
					m_columnStarts[2] = AddObjectControls(Columns[2], m_columnStarts[2], ManagerSingleton<SM>.Instance, new string[] { "SetReverbEnvironment" }, null, null, new bool[] { true }, new object[][] { new object[] { m_soundEnv } });
					m_columnStarts[2] = AddObjectControls(Columns[2], m_columnStarts[2], this, new string[] { "m_soundEnv" });
				}
			}
		}

		public override void PageTick()
		{
			base.PageTick();

			if (GM.CurrentPlayerBody != null)
				m_playerIFF = GM.CurrentPlayerBody.GetPlayerIFF();
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
	}
}
