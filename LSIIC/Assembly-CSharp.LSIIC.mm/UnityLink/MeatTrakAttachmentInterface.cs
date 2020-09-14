using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace LSIIC
{
	public class MeatTrakAttachmentInterface : FVRFireArmAttachmentInterface
	{
		[Header("MeatTrak Interface")]
		public MeatTrak MeatTrak;

		public enum TrackingModes
		{
			None,
			Kills,
			Shots,
			Reloads,
			Bullets
		}
		public TrackingModes TrackingMode = TrackingModes.None;
		public Sprite[] ModeSprites;
		public Renderer ModeDiplayRenderer;

		public AudioEvent Aud_ModeSwitch;
		public AudioEvent Aud_ClearRequestConfirmation;
		public AudioEvent Aud_ClearComplete;

		private bool m_waitingForConfirmation;
		private Texture2D[] m_modeTextures;

		public void Awake()
		{
			base.Awake();

			m_modeTextures = new Texture2D[ModeSprites.Length];
			for (int i = 0; i < ModeSprites.Length; i++)
				m_modeTextures[i] = MeatTrak.ConvertSpriteToTexture(ModeSprites[i]);

			UpdateMode();

#if !UNITY_EDITOR && !UNITY_STANDALONE
			GM.CurrentSceneSettings.KillEvent += wwBotKillEvent;
			GM.CurrentSceneSettings.SosigKillEvent += SosigKillEvent;
			GM.CurrentSceneSettings.ShotFiredEvent += ShotFiredEvent;
			GM.CurrentSceneSettings.FireArmReloadedEvent += FireArmReloadedEvent;
#endif
		}

		public void Update()
		{
			switch (TrackingMode)
			{
				case TrackingModes.Bullets:
					if (Attachment != null && Attachment.GetRootObject() != null && Attachment.GetRootObject() is FVRFireArm)
						UpdateBulletMode((FVRFireArm)Attachment.GetRootObject());
					break;
			}
		}

		public void OnDestroy()
		{
#if !UNITY_EDITOR && !UNITY_STANDALONE
			GM.CurrentSceneSettings.KillEvent -= wwBotKillEvent;
			GM.CurrentSceneSettings.SosigKillEvent -= SosigKillEvent;
			GM.CurrentSceneSettings.ShotFiredEvent -= ShotFiredEvent;
			GM.CurrentSceneSettings.FireArmReloadedEvent -= FireArmReloadedEvent;
#endif

			base.OnDestroy();
		}

		public override void UpdateInteraction(FVRViveHand hand)
		{
			base.UpdateInteraction(hand);

			if (hand.Input.TouchpadDown && hand.Input.TouchpadAxes.magnitude > 0.25f && MeatTrak != null)
			{
				Vector2 touchpadAxes = hand.Input.TouchpadAxes;

				if (Vector2.Angle(hand.Input.TouchpadAxes, Vector2.up) <= 45f)
				{
					if (m_waitingForConfirmation)
					{
						MeatTrak.NumberTarget = 0;

						if (Aud_ClearComplete.Clips.Count > 0)
							SM.PlayCoreSound(FVRPooledAudioType.UIChirp, Aud_ClearComplete, this.transform.position);
					}
					else
					{
						if (Aud_ClearRequestConfirmation.Clips.Count > 0)
							SM.PlayCoreSound(FVRPooledAudioType.UIChirp, Aud_ClearRequestConfirmation, this.transform.position);
					}

					m_waitingForConfirmation = !m_waitingForConfirmation;
				}

				if (Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) <= 45f || Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) <= 45f)
				{
					int direction = (int)Mathf.Sign(touchpadAxes.x);
					TrackingMode = (TrackingModes)Mathf.Repeat((float)TrackingMode + direction, ModeSprites.Length);

					UpdateMode();

					if (Aud_ModeSwitch.Clips.Count > 0)
						SM.PlayCoreSound(FVRPooledAudioType.UIChirp, Aud_ModeSwitch, this.transform.position);
				}
			}
		}

		public override void EndInteraction(FVRViveHand hand)
		{
			base.EndInteraction(hand);

			m_waitingForConfirmation = false;
		}

		[ContextMenu("UpdateMode")]
		public void UpdateMode()
		{
			if (ModeDiplayRenderer != null && ModeDiplayRenderer.materials.Length <= 2 && m_modeTextures.Length > 0 && m_modeTextures.Length >= (int)TrackingMode)
				ModeDiplayRenderer.materials[1].SetTexture("_MainTex", m_modeTextures[(int)TrackingMode]);
		}

#if !UNITY_EDITOR && !UNITY_STANDALONE
		private void FireArmReloadedEvent(FVRObject obj)
		{
			if (obj == null)
				return;

			if (Attachment != null && Attachment.GetRootObject() != null && Attachment.GetRootObject().ObjectWrapper == obj)
			{
				if (MeatTrak != null && TrackingMode == TrackingModes.Reloads)
					MeatTrak.NumberTarget++;
			}
		}

		private void ShotFiredEvent(FVRFireArm firearm)
		{
			if (firearm == null)
				return;

			if (Attachment != null && Attachment.GetRootObject() != null && Attachment.GetRootObject() == firearm)
			{
				if (MeatTrak != null && TrackingMode == TrackingModes.Shots)
					MeatTrak.NumberTarget++;
				else if (TrackingMode == TrackingModes.Bullets)
					UpdateBulletMode(firearm);
			}
		}

		private void SosigKillEvent(Sosig sosig)
		{
			if (sosig == null)
				return;

			if (Attachment != null && Attachment.GetRootObject() != null && Attachment.GetRootObject() is FVRFireArm && Attachment.GetRootObject().m_hand != null)
			{
				//this is dirty as it counts all sosigs dying while the attachment is on a gun
				if (sosig.E.IFFCode != sosig.GetDiedFromIFF() && MeatTrak != null && TrackingMode == TrackingModes.Kills)
					MeatTrak.NumberTarget++;
			}
		}

		private void wwBotKillEvent(Damage dam)
		{
			if (dam == null)
				return;

			if (Attachment != null && Attachment.GetRootObject() != null && Attachment.GetRootObject() is FVRFireArm && Attachment.GetRootObject().m_hand != null)
			{
				if (dam.Source_IFF == GM.CurrentPlayerBody.GetPlayerIFF() && MeatTrak != null && TrackingMode == TrackingModes.Kills)
					MeatTrak.NumberTarget++;
			}
		}

		private void UpdateBulletMode(FVRFireArm firearm)
		{
			if (Attachment != null && Attachment.GetRootObject() != null && Attachment.GetRootObject() == firearm)
			{
				MeatTrak.NumberTarget = 0;

				FVRFireArmMagazine mag = firearm.GetComponentInChildren<FVRFireArmMagazine>();
				if (mag != null)
					MeatTrak.NumberTarget = mag.m_numRounds;

				FVRFireArmClip clip = firearm.GetComponentInChildren<FVRFireArmClip>();
				if (clip != null)
					MeatTrak.NumberTarget = clip.m_numRounds;

				if (firearm is FVRFireArm)
				{
					if (firearm is BreakActionWeapon)
					{
						BreakActionWeapon baw = firearm as BreakActionWeapon;
						for (int j = 0; j < baw.Barrels.Length; j++)
							if (baw.Barrels[j].Chamber.IsFull && !baw.Barrels[j].Chamber.IsSpent)
								MeatTrak.NumberTarget += 1;
					}
					else if (firearm is Derringer)
					{
						Derringer derringer = firearm as Derringer;
						for (int j = 0; j < derringer.Barrels.Count; j++)
							if (derringer.Barrels[j].Chamber.IsFull && !derringer.Barrels[j].Chamber.IsSpent)
								MeatTrak.NumberTarget += 1;
					}
					else if (firearm is SingleActionRevolver)
					{
						SingleActionRevolver saRevolver = firearm as SingleActionRevolver;
						for (int j = 0; j < saRevolver.Cylinder.Chambers.Length; j++)
							if (saRevolver.Cylinder.Chambers[j].IsFull && !saRevolver.Cylinder.Chambers[j].IsSpent)
								MeatTrak.NumberTarget += 1;
					}

					if (firearm.GetType().GetField("Chamber") != null) //handles most guns
					{
						FVRFireArmChamber Chamber = (FVRFireArmChamber)firearm.GetType().GetField("Chamber").GetValue(firearm);
						if (Chamber.IsFull && !Chamber.IsSpent)
							MeatTrak.NumberTarget += 1;
					}
					if (firearm.GetType().GetField("Chambers") != null) //handles Revolver, LAPD2019, RevolvingShotgun
					{
						FVRFireArmChamber[] Chambers = (FVRFireArmChamber[])firearm.GetType().GetField("Chambers").GetValue(firearm);
						for (int j = 0; j < Chambers.Length; j++)
							if (Chambers[j].IsFull && !Chambers[j].IsSpent)
								MeatTrak.NumberTarget += 1;
					}
				}
			}
		}
#endif
	}
}
