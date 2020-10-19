using FistVR;
using LSIIC.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace LSIIC.ModPanel
{
	public class ModPanelV2 : FVRPhysicalObject, IFVRDamageable
	{
		[Header("ModPanelV2")]
		public Canvas Canvas;
		public Text BackgroundText;
		public Text HeldObjectsText;

		[Header("Pages")]
		public Text PageNameText;
		public List<GameObject> PagePrefabs;
		[HideInInspector]
		public List<ModPanelV2Page> Pages = new List<ModPanelV2Page>();
		public Dictionary<Type, ModPanelV2Page> PagesByType = new Dictionary<Type, ModPanelV2Page>();

		public List<GameObject> ControlPrefabs;

		private int m_pageIndex = 0;

		public void Awake()
		{
			base.Awake();

			foreach (GameObject prefab in PagePrefabs)
			{
				ModPanelV2Page page = Instantiate(prefab, Canvas.transform).GetComponent<ModPanelV2Page>();
				page.gameObject.SetActive(false);
				page.Panel = this;
				page.PageInit();
				Pages.Add(page);
				if (!PagesByType.ContainsKey(page.GetType()))
					PagesByType[page.GetType()] = page;
			}

			SwitchPage(0);
		}

		public override void UpdateInteraction(FVRViveHand hand)
		{
			base.UpdateInteraction(hand);

			if (hand.Input.TouchpadDown && hand.Input.TouchpadAxes.magnitude > 0.25f)
			{
				Vector2 touchpadAxes = hand.Input.TouchpadAxes;

				if (Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) <= 45f)
					PrevPage();
				else if (Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) <= 45f)
					NextPage();

				if (Vector2.Angle(touchpadAxes, Vector2.down) <= 45f)
					ToggleKinematicLocked();
			}
		}

		public void Update()
		{
#if !UNITY_EDITOR && !UNITY_STANDALONE
			if (GM.CurrentPlayerBody.Head != null && Vector3.Dot(GM.CurrentPlayerBody.Head.position, this.transform.position) > 0)
#endif
				if (Pages.Count > m_pageIndex && Pages[m_pageIndex] != null)
					Pages[m_pageIndex].PageTick();

			if (BackgroundText != null)
				BackgroundText.text = Helpers.H3InfoPrint(Helpers.H3Info.All);

			if (HeldObjectsText != null)
				HeldObjectsText.text = LSIIC.Core.Helpers.GetHeldObjects();

#if UNITY_EDITOR || UNITY_STANDALONE
			if (Input.GetKeyDown(KeyCode.Alpha1))
				PrevPage();
			if (Input.GetKeyDown(KeyCode.Alpha2))
				NextPage();
#endif
		}

		public void SwitchPage(int index)
		{
			index = (int)Mathf.Repeat(index, Pages.Count);

			Pages[m_pageIndex].PageClose();
			Pages[m_pageIndex].gameObject.SetActive(false);
			Pages[index].gameObject.SetActive(true);
			Pages[index].PageOpen();
			if (PageNameText != null)
				PageNameText.text = Pages[index].PageTitle;
			m_pageIndex = index;
		}

		public void SwitchPage(ModPanelV2Page page)
		{
			int index = Pages.IndexOf(page);
			if (index == -1)
			{
				Debug.LogError("Page does not exist to panel");
				return;
			}

			Pages[m_pageIndex].PageClose();
			Pages[m_pageIndex].gameObject.SetActive(false);
			Pages[index].gameObject.SetActive(true);
			Pages[index].PageOpen();
			if (PageNameText != null)
				PageNameText.text = Pages[index].PageTitle;
			m_pageIndex = index;
		}

		public void SwitchPage(Type pagetype)
		{
			if (!PagesByType.ContainsKey(pagetype))
			{
				Debug.LogError("Page type does not exist in dictionary");
				return;
			}

			Pages[m_pageIndex].PageClose();
			Pages[m_pageIndex].gameObject.SetActive(false);
			PagesByType[pagetype].gameObject.SetActive(true);
			PagesByType[pagetype].PageOpen();
			if (PageNameText != null)
				PageNameText.text = PagesByType[pagetype].PageTitle;
			m_pageIndex = Pages.IndexOf(PagesByType[pagetype]);
		}

		public void PrevPage() { SwitchPage(m_pageIndex - 1); }
		public void NextPage() { SwitchPage(m_pageIndex + 1); }

		public void Damage(Damage dam)
		{
			OnDamage(dam);
		}

		public delegate void EventDamage(Damage dam);
		public event ModPanelV2.EventDamage DamageEvent;
		public void OnDamage(Damage dam)
		{
			if (DamageEvent != null)
				DamageEvent.Invoke(dam);
		}
	}
}
