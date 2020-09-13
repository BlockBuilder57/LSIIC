using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LSIIC.ModPanel
{
	public class ModPanelV2Page_Damage : ModPanelV2Page
	{
		public Text FieldValues;
		public bool AverageDamages;

		public Dictionary<float, Damage> AllDamages = new Dictionary<float, Damage>();
		private Damage m_dmg;

		public override void PageInit()
		{
			base.PageInit();

			AddObjectControl(new Vector2(20, 16), 0, this, "AverageDamages", "Do we averages damages together? Currently {3}");
		}

		public override void PageOpen()
		{
			base.PageOpen();

			if (Panel != null)
				Panel.DamageEvent += Panel_DamageEvent;
		}

		public override void PageClose(bool destroy = false)
		{
			base.PageClose(destroy);

			if (Panel != null)
				Panel.DamageEvent -= Panel_DamageEvent;
		}

		public void UpdateDamageDisplay()
		{
#if !UNITY_EDITOR && !UNITY_STANDALONE
			if (m_dmg != null)
			{
				FieldValues.text = $"{m_dmg.Class}\n{m_dmg.Source_IFF}\n\n{m_dmg.Dam_Blunt}\n{m_dmg.Dam_Piercing}\n{m_dmg.Dam_Cutting}\n{m_dmg.Dam_TotalKinetic}\n{m_dmg.Dam_Thermal}\n{m_dmg.Dam_Chilling}\n{m_dmg.Dam_EMP}\n{m_dmg.Dam_TotalEnergetic}\n{m_dmg.Dam_Stunning}\n{m_dmg.Dam_Blinding}\n\n{m_dmg.point:F3}\n{m_dmg.hitNormal:F3}\n{m_dmg.strikeDir:F3}\n{m_dmg.damageSize}\n\n{AllDamages.Count}";
				if (AllDamages != null && AllDamages.Count > 1)
					FieldValues.text += $"\n{(AllDamages.Count / (AllDamages.Last().Key - AllDamages.First().Key)) * 60f}";
				else
					FieldValues.text += "\n-";
			}
			else
				Debug.LogError("[ModPanelV2Page_Damage] m_dmg == null!");
#endif
		}

		public void ClearDamages()
		{
			if (AllDamages != null)
				AllDamages.Clear();
			FieldValues.text = "-\n-\n\n-\n-\n-\n-\n-\n-\n-\n-\n-\n-\n\n-\n-\n-\n-\n\n-\n-";
		}

		public void Panel_DamageEvent(Damage dam)
		{
			if (!AverageDamages)
				ClearDamages();

			if (AllDamages != null && !AllDamages.ContainsKey(Time.time))
				AllDamages.Add(Time.time, dam);

			if (AllDamages != null && AllDamages.Count > 0)
			{
				m_dmg = new Damage
				{
					Class = AllDamages.Last().Value.Class,
					Source_IFF = AllDamages.Last().Value.Source_IFF,

					Dam_Blunt = AllDamages.Values.Average(x => x.Dam_Blunt),
					Dam_Piercing = AllDamages.Values.Average(x => x.Dam_Piercing),
					Dam_Cutting = AllDamages.Values.Average(x => x.Dam_Cutting),
					Dam_TotalKinetic = AllDamages.Values.Average(x => x.Dam_TotalKinetic),
					Dam_Thermal = AllDamages.Values.Average(x => x.Dam_Thermal),
					Dam_Chilling = AllDamages.Values.Average(x => x.Dam_Chilling),
					Dam_EMP = AllDamages.Values.Average(x => x.Dam_EMP),
					Dam_TotalEnergetic = AllDamages.Values.Average(x => x.Dam_TotalEnergetic),
					Dam_Stunning = AllDamages.Values.Average(x => x.Dam_Stunning),
					Dam_Blinding = AllDamages.Values.Average(x => x.Dam_Blinding),

					point = new Vector3(AllDamages.Values.Average(x => x.point.x), AllDamages.Values.Average(x => x.point.y), AllDamages.Values.Average(x => x.point.z)),
					hitNormal = new Vector3(AllDamages.Values.Average(x => x.hitNormal.x), AllDamages.Values.Average(x => x.hitNormal.y), AllDamages.Values.Average(x => x.hitNormal.z)),
					strikeDir = new Vector3(AllDamages.Values.Average(x => x.strikeDir.x), AllDamages.Values.Average(x => x.strikeDir.y), AllDamages.Values.Average(x => x.strikeDir.z)),
					damageSize = AllDamages.Values.Average(x => x.damageSize)
				};
			}

			UpdateDamageDisplay();
		}
	}
}
