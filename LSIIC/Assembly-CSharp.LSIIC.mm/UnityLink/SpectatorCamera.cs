﻿using FistVR;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LSIIC
{
	public class SpectatorCamera : FVRPhysicalObject
	{
		[Header("Spectator Camera")]
		public Camera DisplayCam;
		public Camera RenderTargetCam;
		public RotateAroundRootAxis Screen;

		[Header("Rendering Settings")]
		public Material ScreenOn;
		public Material ScreenOff;
		public Renderer LEDRenderer;
		public Color LEDColorOn;
		public Color LEDEmissOn;
		public Color LEDColorOff;
		public Color LEDEmissOff;

		[Header("Audio Events")]
		public AudioEvent CamOn;
		public AudioEvent CamOff;
		public AudioEvent FOVChange;
		public AudioEvent KinematicToggle;

		public bool CameraOn { get; private set; }

		public override void BeginInteraction(FVRViveHand hand)
		{
			base.BeginInteraction(hand);

			if (AccessTools.Field(typeof(FVRSceneSettings), "m_previewCam") != null)
			{
				Camera previewCam = (Camera)AccessTools.Field(typeof(FVRSceneSettings), "m_previewCam").GetValue(GM.CurrentSceneSettings);
				if (ScreenOn != null && previewCam != null)
					ScreenOn.SetTexture("_MainTex", previewCam.targetTexture);
			}
		}

		public override void UpdateInteraction(FVRViveHand hand)
		{
			base.UpdateInteraction(hand);

			if (hand.Input.TouchpadDown && hand.Input.TouchpadAxes.magnitude > 0.25f)
			{
				Vector2 touchpadAxes = hand.Input.TouchpadAxes;

				if (Vector2.Angle(touchpadAxes, Vector2.down) <= 45f)
				{
					ToggleKinematicLocked();
					if (KinematicToggle.Clips.Count > 0)
						SM.PlayCoreSound(FVRPooledAudioType.UIChirp, KinematicToggle, this.transform.position);
				}

				if (DisplayCam != null /*&& RenderTargetCam != null*/)
				{
					if (Vector2.Angle(touchpadAxes, Vector2.up) <= 45f)
					{
						ToggleCameraState();
						if (Screen != null)
							Screen.TargetRotation = new Vector3(0f, CameraOn ? 0f : -90f, 0f);
					}

					if (CameraOn && (Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) <= 45f || Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) <= 45f))
					{
						int direction = (int)Mathf.Sign(touchpadAxes.x) * 10;
						GM.Options.ControlOptions.CamFOV = Mathf.Clamp(GM.Options.ControlOptions.CamFOV + direction, 10f, 180f);
						//DisplayCam.fieldOfView = Mathf.Clamp(DisplayCam.fieldOfView + direction, 20, 80);
						//RenderTargetCam.fieldOfView = DisplayCam.fieldOfView;
						if (FOVChange.Clips.Count > 0)
							SM.PlayCoreSound(FVRPooledAudioType.UIChirp, FOVChange, this.transform.position);
					}
				}
			}
		}

		public override void FVRUpdate()
		{
			base.FVRUpdate();
			if (CameraOn && GM.CurrentSceneSettings.GetCamObjectPoint() != DisplayCam.transform)
				UpdateCameraState(false);
		}

		[ContextMenu("Toggle Camera")]
		public void ToggleCameraState()
		{
			UpdateCameraState(!CameraOn);
			if (!CameraOn && GM.CurrentSceneSettings.GetCamObjectPoint() == DisplayCam.transform)
				GM.CurrentSceneSettings.SetCamObjectPoint(null);
		}

		public void UpdateCameraState(bool isOn)
		{
			DisplayCam.gameObject.SetActive(isOn);
			//DisplayCam.enabled = isOn;
			//RenderTargetCam.gameObject.SetActive(isOn);
			//RenderTargetCam.enabled = isOn;
			
			if (isOn)
			{
				if (DisplayCam)
					GM.CurrentSceneSettings.SetCamObjectPoint(DisplayCam.transform);
				GM.Options.ControlOptions.PCamMode = ControlOptions.PreviewCamMode.Enabled;
			}

			if (LEDRenderer != null)
			{
				LEDRenderer.material.SetColor("_Color", isOn ? LEDColorOn : LEDColorOff);
				LEDRenderer.material.SetColor("_EmissionColor", isOn ? LEDEmissOn : LEDEmissOff);
			}
			if (Screen != null)
			{
				Screen.TargetRotation = new Vector3(0f, isOn ? 0f : -90f, 0f);
				Material[] materials = Screen.gameObject.GetComponent<Renderer>().materials;
				if (materials.Length > 1)
					materials[1] = isOn ? ScreenOn : ScreenOff;
				Screen.gameObject.GetComponent<Renderer>().materials = materials;
			}

			/*foreach (SpectatorCamera cam in FindObjectsOfType<SpectatorCamera>())
			{
				if (cam != this && cam.CameraOn)
					cam.UpdateCameraState(false);
			}
			foreach (SpectatorCameraAttachmentInterface cam in FindObjectsOfType<SpectatorCameraAttachmentInterface>())
			{
				if (cam != this && cam.CameraOn)
					cam.UpdateCameraState(false);
			}*/

			if (isOn && CamOn.Clips.Count > 0)
				SM.PlayCoreSound(FVRPooledAudioType.UIChirp, CamOn, this.transform.position);
			else if (CamOff.Clips.Count > 0)
				SM.PlayCoreSound(FVRPooledAudioType.UIChirp, CamOff, this.transform.position);

			CameraOn = isOn;
		}
	}
}
