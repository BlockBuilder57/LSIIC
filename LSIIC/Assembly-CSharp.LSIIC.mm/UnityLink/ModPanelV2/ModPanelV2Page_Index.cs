#if !UNITY_EDITOR && !UNITY_STANDALONE
using LSIIC.Core;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LSIIC.ModPanel
{
	public class ModPanelV2Page_Index : ModPanelV2Page
	{
		[Header("Index Page")]
		public Text H3InfoText;
		public Text[] PageButtons;

		public override void PageOpen()
		{
			base.PageOpen();

			if (Panel != null)
			{
				foreach (Text page in PageButtons)
					page.gameObject.SetActive(false);

				for (int i = 0; i < Panel.Pages.Count; i++)
				{
					PageButtons[i].text = Panel.Pages[i].PageTitle;
					PageButtons[i].gameObject.SetActive(true);
				}
			}
		}

		public override void PageTick()
		{
			base.PageTick();

#if !UNITY_EDITOR && !UNITY_STANDALONE
			if (H3InfoText != null)
				H3InfoText.text = Helpers.H3InfoPrint(Helpers.H3Info.All);
#endif
		}

		public void GotoPanelPage(int page)
		{
			if (Panel != null)
				Panel.SwitchPage(page);
		}
	}
}
