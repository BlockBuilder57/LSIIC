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
		public override GameObject DuplicateFromSpawnLock(FVRViveHand hand)
		{
			GameObject gameObject = Instantiate(this.ObjectWrapper.GetGameObject(), this.Transform.position, this.Transform.rotation);
			Banger banger = gameObject.GetComponent<Banger>();
			hand.ForceSetInteractable(banger);
			banger.SetQuickBeltSlot(null);
			banger.BeginInteraction(hand);

			banger.Payloads.AddRange(this.Payloads);
			banger.Shrapnel.AddRange(this.Shrapnel);
			banger.ShrapnelLeftToFire.AddRange(this.ShrapnelLeftToFire);

			banger.PowerupSplodes.AddRange(this.PowerupSplodes);

			banger.descrip = this.descrip;

			banger.m_isSticky = this.m_isSticky;
			banger.m_isSilent = this.m_isSilent;
			banger.m_isHoming = this.m_isHoming;
			banger.m_canbeshot = this.m_canbeshot;

			banger.m_shrapnelVel = this.m_shrapnelVel;

			if (this.m_colliders[0].material.bounciness == PhysMat_Bouncy.bounciness)
				banger.SetToBouncy();

			banger.ThrowVelMultiplier = this.ThrowVelMultiplier;
			banger.ThrowAngMultiplier = this.ThrowAngMultiplier;
			banger.RootRigidbody.drag = this.RootRigidbody.drag;
			banger.RootRigidbody.angularDrag = this.RootRigidbody.angularDrag;

			banger.Complete();

			foreach (BangerDetonator det in FindObjectsOfType<BangerDetonator>())
				det.RegisterBanger(banger);

			return gameObject;
		}
	}
}
