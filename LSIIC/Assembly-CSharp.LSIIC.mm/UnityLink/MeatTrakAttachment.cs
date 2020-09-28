using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace LSIIC
{
	public class MeatTrakAttachment : FVRFireArmAttachment
	{
		public void Awake()
		{
			base.Awake();

			MeatTrakAttachmentInterface meatInterface = AttachmentInterface as MeatTrakAttachmentInterface;
			if (meatInterface != null && meatInterface.MeatTrak != null)
			{
				//call these early so we don't have to wait for the interface
				meatInterface.InitInterface();
				meatInterface.MeatTrak.InitDisplay();
			}
		}

		public override void ConfigureFromFlagDic(Dictionary<string, string> f)
		{
			if (f == null)
				return;

			MeatTrakAttachmentInterface meatInterface = AttachmentInterface as MeatTrakAttachmentInterface;
			if (meatInterface != null)
			{
				meatInterface.TrackingMode = (MeatTrakAttachmentInterface.TrackingModes)Enum.Parse(typeof(MeatTrakAttachmentInterface.TrackingModes), f["TrackingMode"]);
				meatInterface.UpdateMode();
				if (meatInterface.MeatTrak != null)
				{
					meatInterface.MeatTrak.NumberTarget = float.Parse(f["NumberTarget"]);
					meatInterface.MeatTrak.NumberDisplay = meatInterface.MeatTrak.NumberTarget;
				}
			}
		}

		public override Dictionary<string, string> GetFlagDic()
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			MeatTrakAttachmentInterface meatInterface = AttachmentInterface as MeatTrakAttachmentInterface;
			if (meatInterface != null)
			{
				dictionary.Add("TrackingMode", meatInterface.TrackingMode.ToString());
				if (meatInterface.MeatTrak != null)
					dictionary.Add("NumberTarget", meatInterface.MeatTrak.NumberTarget.ToString());
			}
			return dictionary;
		}
	}
}
