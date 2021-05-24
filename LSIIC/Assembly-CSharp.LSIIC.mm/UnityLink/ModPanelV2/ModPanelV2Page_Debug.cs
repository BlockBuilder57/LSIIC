using FistVR;
using LSIIC.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LSIIC.ModPanel
{
	public class ModPanelV2Page_Debug : ModPanelV2Page
	{
		[Header("Debug Page")]
		public Transform RaycastCylinder;
		public Transform CylinderBack;
		public Transform CylinderForward;

		[Space()]
		public Text RaycastHitPath;
		public Text RaycastHitInfo;
		public Text RaycastObjectInfo;

		[Space()]
		public Text ButtonSelectText;
		public GameObject SelectedControls;

		private Vector3 m_cylinderTarget;

		private RaycastHit m_raycastHit;
		private Collider m_raycastHitLastCollider;

		private GameObject m_selection;
		private Transform m_selectionParent;

		private LayerMask m_layerMask = new LayerMask();
		private const int LAYERMASK_H3 = 532481; // Default, AgentMeleeWeapon, Environment
		private const int LAYERMASK_ALL = int.MaxValue; // all

		public void Awake()
		{
			m_layerMask.value = LAYERMASK_ALL;
			if (this.Panel != null) // just so we're not constantly refreshing nothing
				m_raycastHitLastCollider = this.Panel.GetComponentInChildren<Collider>();
		}

		public override void PageTick()
		{
			base.PageTick();

			if (RaycastCylinder == null || RaycastCylinder.parent == null)
				return;

			if (m_selection == null)
			{
				if (Physics.Raycast(RaycastCylinder.parent.position, RaycastCylinder.parent.forward, out m_raycastHit, 99999f, m_layerMask, QueryTriggerInteraction.Collide))
				{
					m_cylinderTarget = m_raycastHit.point;
					
					// must always update
					if (RaycastHitInfo != null)
						RaycastHitInfo.text = "Hit at " + m_raycastHit.point.ToString("F2") + ", " + m_raycastHit.distance.ToString("F2") + "m away. LayerMask of " + m_layerMask.value.ToString();

					if (m_raycastHitLastCollider != m_raycastHit.collider)
					{
						m_raycastHitLastCollider = m_raycastHit.collider;

						if (RaycastHitPath != null)
							RaycastHitPath.text = Helpers.GetObjectHierarchyPath(m_raycastHit.collider.transform);
						if (RaycastObjectInfo != null)
							RaycastObjectInfo.text = Helpers.GetObjectInfo(m_raycastHit.collider.gameObject, m_raycastHit.collider.attachedRigidbody);

						if (ButtonSelectText != null)
							ButtonSelectText.gameObject.SetActive(true);
					}
				}
				else if (m_raycastHitLastCollider != null)
				{
					m_raycastHitLastCollider = null;
					m_cylinderTarget = Vector3.zero;

					if (RaycastHitInfo != null)
						RaycastHitInfo.text = string.Empty;
					if (RaycastHitPath != null)
						RaycastHitPath.text = string.Empty;
					if (RaycastObjectInfo != null)
						RaycastObjectInfo.text = string.Empty;

					if (ButtonSelectText != null)
						ButtonSelectText.gameObject.SetActive(false);
				}

				RaycastCylinder.localRotation = Quaternion.identity;
			}
			else
			{
				if (RaycastHitInfo != null)
					RaycastHitInfo.text = "Selection at " + m_selection.transform.position.ToString("F2") + ". LayerMask of " + m_layerMask.value.ToString();
				// RaycastHitPath and RaycastObjectInfo are left null because they were last updated with either the raycast or the SelectObject function

				m_cylinderTarget = m_selection.transform.position;
				RaycastCylinder.LookAt(m_selection.transform);
			}

			RaycastCylinder.gameObject.SetActive(m_cylinderTarget != Vector3.zero);
			RaycastCylinder.localScale = new Vector3(5f, 5f, Vector3.Distance(m_cylinderTarget, this.transform.position) * (RaycastCylinder.localScale.x / RaycastCylinder.lossyScale.x));
		}

		public void SelectOrReleaseObject()
		{
			if (m_selection == null && m_raycastHitLastCollider != null)
				SelectObject(m_raycastHitLastCollider.transform);
			else
				ReleaseObject();
		}

		public void SelectObject(Transform obj)
		{
			if (m_selection != null)
				ReleaseObject();

			if (obj != null)
			{
				m_selection = obj.gameObject;
				m_selectionParent = obj.parent;

				if (ButtonSelectText != null)
					ButtonSelectText.text = "Release Object";
				if (SelectedControls != null)
					SelectedControls.SetActive(true);

				if (RaycastHitPath != null)
					RaycastHitPath.text = Helpers.GetObjectHierarchyPath(obj.transform);
				if (RaycastObjectInfo != null)
					RaycastObjectInfo.text = Helpers.GetObjectInfo(obj.gameObject, m_raycastHitLastCollider.attachedRigidbody);
			}
		}

		public void SelectObjectParent()
		{
			if (m_selectionParent != null)
				SelectObject(m_selectionParent);
		}

		public void ReleaseObject()
		{
			if (m_selection != null && m_selection.transform.parent != m_selectionParent)
				m_selection.transform.SetParent(m_selectionParent);

			m_selection = null;
			m_selectionParent = null;
			RaycastCylinder.gameObject.SetActive(false);

			if (Panel.RootRigidbody != null)
				Panel.RootRigidbody.ResetCenterOfMass();

			if (ButtonSelectText != null)
				ButtonSelectText.text = "Select Object";
			if (SelectedControls != null)
				SelectedControls.SetActive(false);
		}

		public void ToggleRaycastMask()
		{
			m_layerMask.value = m_layerMask.value == LAYERMASK_ALL ? LAYERMASK_H3 : LAYERMASK_ALL;
		}

		public void ToggleRaycastDirection()
		{
			if (RaycastCylinder.parent == CylinderBack)
				RaycastCylinder.SetParent(CylinderForward);
			else
				RaycastCylinder.SetParent(CylinderBack);
		}

		public void DeleteObject()
		{
			if (m_selection != null)
			{
				if (m_selection.GetComponent<FVRInteractiveObject>() != null)
				{
					FVRInteractiveObject interobj = m_selection.GetComponent<FVRInteractiveObject>();
					if (interobj.IsHeld)
					{
						interobj.EndInteraction(interobj.m_hand);
						interobj.m_hand.ForceSetInteractable(null);
					}
				}

				Destroy(m_selection.gameObject);
				ReleaseObject();
			}
		}

		public void GrabOrReleaseObject()
		{
			if (m_selection != null)
			{
				m_selection.transform.SetParent(m_selection.transform.parent == this.Panel.transform ? m_selectionParent : this.Panel.transform);
				if (m_raycastHitLastCollider != null && m_raycastHitLastCollider.attachedRigidbody != null)
					m_raycastHitLastCollider.attachedRigidbody.isKinematic = m_selection.transform.parent == this.Panel.transform;
			}
		}

		public void OpenObjectInPanelSpawnerPage()
		{
			if (m_selection != null && Panel != null)
			{
				ModPanelV2Page_Spawner spawner = (ModPanelV2Page_Spawner)Panel.PagesByType[typeof(ModPanelV2Page_Spawner)];
				Panel.SwitchPage(spawner);
				spawner.UpdateCurrentGameObj(m_selection);
			}
		}

	}
}
