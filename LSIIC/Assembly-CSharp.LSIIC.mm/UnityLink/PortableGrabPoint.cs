using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LSIIC
{
	public class PortableGrabPoint : FVRPhysicalObject
	{
		[Header("PortableGrabPoint")]
		public FVRHandGrabPoint GrabPoint;
		public Renderer GeoRenderer;

		[ColorUsage(false, true, 0f, 8f, 0.125f, 3f)]
		public Color RingColorInactive = Color.black;
		[ColorUsage(false, true, 0f, 8f, 0.125f, 3f)]
		public Color RingColorActive = Color.white;

		private FVRViveHand m_lastHand;
		private bool m_grabPointActive;

		private float m_timeSincePickup = 0f;

		public override void BeginInteraction(FVRViveHand hand)
		{
			base.BeginInteraction(hand);
		}

		public override void UpdateInteraction(FVRViveHand hand)
		{
			base.UpdateInteraction(hand);

			m_timeSincePickup += Time.deltaTime;

			if (m_timeSincePickup >= 0.1f && hand.Input.TouchpadDown && GrabPoint != null && !m_grabPointActive)
			{
				SetIsKinematicLocked(true);
				hand.ForceSetInteractable(GrabPoint);
				GrabPoint.BeginInteraction(hand);
				m_lastHand = hand;
				m_grabPointActive = true;
			}
		}

		protected override void FVRUpdate()
		{
			base.FVRUpdate();

			if (QuickbeltSlot != null)
				m_grabPointActive = false;

			if (m_grabPointActive && m_lastHand != null && m_lastHand.CurrentInteractable != GrabPoint && GrabPoint.m_hand == null && IsKinematicLocked)
			{
				SetIsKinematicLocked(false);
				if (m_lastHand != null)
				{
					m_lastHand.ForceSetInteractable(this);
					BeginInteraction(m_lastHand);
				}
				m_grabPointActive = false;
			}

			if (m_timeSincePickup > 0f && !(m_grabPointActive || m_hand != null))
				m_timeSincePickup -= Time.deltaTime;

			if (GeoRenderer != null && GeoRenderer.material != null)
				GeoRenderer.material.SetColor("_EmissionColor", Color.Lerp(RingColorInactive, RingColorActive, m_timeSincePickup));
		}
	}
}
