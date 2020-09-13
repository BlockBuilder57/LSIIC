using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LSIIC
{
    public class SpectatorCameraAttachmentInterface : FVRFireArmAttachmentInterface
    {
        [Header("Spectator Camera")]
        public Camera DisplayCam;

        [Header("Rendering Settings")]
        public Renderer LEDRenderer;
        public Color LEDColorOn;
        public Color LEDEmissOn;
        public Color LEDColorOff;
        public Color LEDEmissOff;

        [Header("Audio Events")]
        public AudioEvent CamOn;
        public AudioEvent CamOff;
        public AudioEvent FOVChange;

        public bool CameraOn { get; private set; }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);

            if (hand.Input.TouchpadDown && hand.Input.TouchpadAxes.magnitude > 0.25f)
            {
                Vector2 touchpadAxes = hand.Input.TouchpadAxes;

                if (DisplayCam != null)
                {
                    if (Vector2.Angle(touchpadAxes, Vector2.up) <= 45f)
                        ToggleCameraState();

                    if (CameraOn && (Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) <= 45f || Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) <= 45f))
                    {
                        int direction = (int)Mathf.Sign(touchpadAxes.x) * 10;
                        DisplayCam.fieldOfView = Mathf.Clamp(DisplayCam.fieldOfView + direction, 20, 80);
                        if (FOVChange.Clips.Count > 0)
                            SM.PlayCoreSound(FVRPooledAudioType.UIChirp, FOVChange, this.transform.position);
                    }
                }
            }
        }

        [ContextMenu("Toggle Camera")]
        public void ToggleCameraState()
        {
            UpdateCameraState(!CameraOn);
        }

        public void UpdateCameraState(bool isOn)
        {
            DisplayCam.gameObject.SetActive(isOn);
            DisplayCam.enabled = isOn;

            if (LEDRenderer != null)
            {
                LEDRenderer.material.SetColor("_Color", isOn ? LEDColorOn : LEDColorOff);
                LEDRenderer.material.SetColor("_EmissionColor", isOn ? LEDEmissOn : LEDEmissOff);
            }

            foreach (SpectatorCamera cam in FindObjectsOfType<SpectatorCamera>())
            {
                if (cam != this && cam.CameraOn)
                    cam.UpdateCameraState(false);
            }
            foreach (SpectatorCameraAttachmentInterface cam in FindObjectsOfType<SpectatorCameraAttachmentInterface>())
            {
                if (cam != this && cam.CameraOn)
                    cam.UpdateCameraState(false);
            }

            if (isOn && CamOn.Clips.Count > 0)
                SM.PlayCoreSound(FVRPooledAudioType.UIChirp, CamOn, this.transform.position);
            else if (CamOff.Clips.Count > 0)
                SM.PlayCoreSound(FVRPooledAudioType.UIChirp, CamOff, this.transform.position);

            CameraOn = isOn;
        }
    }
}
