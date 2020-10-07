using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace FistVR
{
	public class patch_Banger : Banger
	{
		public string descrip;

		public List<Banger.PowerUpSplode> PowerupSplodes;

		public bool m_isSticky;
		public bool m_isSilent;
		public bool m_isHoming;
		public bool m_canbeshot;

		public Vector2 m_shrapnelVel;

		public override GameObject DuplicateFromSpawnLock(FVRViveHand hand)
		{
			GameObject gameObject = Instantiate(ObjectWrapper.GetGameObject(), Transform.position, Transform.rotation);
			Banger banger = gameObject.GetComponent<Banger>();
			hand.ForceSetInteractable(banger);
			banger.SetQuickBeltSlot(null);
			banger.BeginInteraction(hand);

			banger.Payloads.AddRange(Payloads);
			banger.Shrapnel.AddRange(Shrapnel);
			banger.ShrapnelLeftToFire.AddRange(ShrapnelLeftToFire);

			FieldInfo splodesRef = AccessTools.Field(typeof(Banger), "PowerupSplodes");
			if (splodesRef != null)
			{
				List<Banger.PowerUpSplode> OtherPowerupSplodes = (List<PowerUpSplode>)splodesRef.GetValue(banger);
				OtherPowerupSplodes.AddRange(PowerupSplodes);
				splodesRef.SetValue(banger, OtherPowerupSplodes);
			}
			
			AccessTools.Field(typeof(Banger), "descrip").SetValue(banger, descrip);

			AccessTools.Field(typeof(Banger), "m_isSticky").SetValue(banger, m_isSticky);
			AccessTools.Field(typeof(Banger), "m_isSilent").SetValue(banger, m_isSilent);
			AccessTools.Field(typeof(Banger), "m_isHoming").SetValue(banger, m_isHoming);
			AccessTools.Field(typeof(Banger), "m_canbeshot").SetValue(banger, m_canbeshot);

			AccessTools.Field(typeof(Banger), "m_shrapnelVel").SetValue(banger, m_shrapnelVel);

			if (m_colliders[0].material.bounciness == PhysMat_Bouncy.bounciness)
				AccessTools.Method(typeof(Banger), "SetToBouncy").Invoke(banger, null);

			banger.ThrowVelMultiplier = ThrowVelMultiplier;
			banger.ThrowAngMultiplier = ThrowAngMultiplier;
			banger.RootRigidbody.drag = RootRigidbody.drag;
			banger.RootRigidbody.angularDrag = RootRigidbody.angularDrag;

			banger.Complete();

			foreach (BangerDetonator det in FindObjectsOfType<BangerDetonator>())
				det.RegisterBanger(banger);

			return gameObject;
		}
	}
}
