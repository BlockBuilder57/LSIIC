using FistVR;
using LSIIC.Core;
using RootMotion;
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
		public Transform RaycastCylinder;
		public LayerMask RaycastMask;
		public Text RaycastHitPath;
		public Text RaycastHitInfo;
		public Text RaycastObjectInfo;

		public GameObject OpenObjectInSpawnerButton;

		private RaycastHit m_raycastHit;
		private Transform m_raycastHitLastTransform;
		private bool m_raycastMaskAll = false;
		private LayerMask m_layermaskAll = new LayerMask();

		public void Awake()
		{
			m_layermaskAll.value = int.MaxValue;
		}

		public override void PageTick()
		{
			base.PageTick();

			if (RaycastHitPath == null || RaycastHitInfo == null || RaycastObjectInfo == null)
				return;

			if (RaycastCylinder != null && Physics.Raycast(RaycastCylinder.position, RaycastCylinder.forward, out m_raycastHit, 99999f, m_raycastMaskAll ? m_layermaskAll : RaycastMask, QueryTriggerInteraction.Collide))
			{
				RaycastCylinder.gameObject.SetActive(true);
				RaycastCylinder.localScale = new Vector3(5f, 5f, m_raycastHit.distance * (RaycastCylinder.localScale.x / RaycastCylinder.lossyScale.x));
				if (RaycastHitInfo != null)
					RaycastHitInfo.text = "Hit at " + m_raycastHit.point.ToString("F2") + ", " + m_raycastHit.distance.ToString("F2") + "m away. LayerMask of " + (m_raycastMaskAll ? m_layermaskAll : RaycastMask).value.ToString();
				if (RaycastHitPath != null)
					RaycastHitPath.text = Helpers.GetHierarchyPath(m_raycastHit.collider.transform);

				if (m_raycastHit.transform == m_raycastHitLastTransform)
					return;
				m_raycastHitLastTransform = m_raycastHit.collider.transform;

				if (OpenObjectInSpawnerButton != null)
					OpenObjectInSpawnerButton.SetActive(m_raycastHitLastTransform != null);

#if !UNITY_EDITOR && !UNITY_STANDALONE
				string info = "";
				info += "Collider object properties:";
				foreach (Component comp in m_raycastHitLastTransform.GetComponents<Component>())
				{
					Type t = comp.GetType();
					bool firstClass = true;
					while (t != null && (firstClass || !t.Namespace.StartsWith("UnityEngine")))
					{
						info += firstClass ? "\nType: " + t.ToString() : " : " + t.ToString();
						t = t.BaseType;
						firstClass = false;
					}
				}
				info += $"\nLayer(s): {LayerMask.LayerToName(m_raycastHitLastTransform.gameObject.layer)}";
				info += $"\nTag: {m_raycastHitLastTransform.gameObject.tag}";

				if (m_raycastHit.collider.attachedRigidbody != null && m_raycastHit.collider.attachedRigidbody.transform != m_raycastHitLastTransform)
				{
					info += $"\n\nAttached rigidbody: {Helpers.GetHierarchyPath(m_raycastHit.collider.attachedRigidbody.transform)}";
					foreach (Component comp in m_raycastHit.collider.attachedRigidbody.GetComponents<Component>())
					{
						Type t = comp.GetType();
						bool firstClass = true;
						while (t != null && (firstClass || !t.Namespace.StartsWith("UnityEngine")))
						{
							info += firstClass ? "\nType: " + t.ToString() : " : " + t.ToString();
							t = t.BaseType;
							firstClass = false;
						}
					}
					info += $"\nLayer(s): {LayerMask.LayerToName(m_raycastHit.collider.attachedRigidbody.gameObject.layer)}";
					info += $"\nTag: {m_raycastHit.collider.attachedRigidbody.gameObject.tag}";
				}

				RaycastObjectInfo.text = info;
#endif
			}
			else
			{
				if (RaycastHitPath != null)
					RaycastHitPath.text = string.Empty;
				if (RaycastCylinder != null)
					RaycastCylinder.gameObject.SetActive(false);
				if (OpenObjectInSpawnerButton != null)
					OpenObjectInSpawnerButton.SetActive(false);
			}
		}

		public void ToggleRaycastMask()
		{
			m_raycastMaskAll = !m_raycastMaskAll;
		}

		public void ToggleRaycastDirection()
		{
			//yeah, I know this sucks and is very based on the setup of the panel
			//however, in the ever eternal words of Valve: Too bad!
			Vector3 position = RaycastCylinder.transform.localPosition;
			Vector3 rotation = RaycastCylinder.transform.localEulerAngles;
			position.z = -position.z;
			rotation.x += 180f;

			RaycastCylinder.transform.localPosition = position;
			RaycastCylinder.localEulerAngles = rotation;
		}

		public void OpenObjectInSpawnerPage()
		{
			if (m_raycastHitLastTransform != null && Panel != null)
			{
				ModPanelV2Page_Spawner spawner = (ModPanelV2Page_Spawner)Panel.PagesByType[typeof(ModPanelV2Page_Spawner)];
				Panel.SwitchPage(spawner);
				spawner.UpdateCurrentGameObj(m_raycastHitLastTransform.gameObject);
			}
		}
	}
}
