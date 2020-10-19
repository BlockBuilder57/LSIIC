using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Alloy;
using FistVR;
using HarmonyLib;
using UnityEngine;
using Valve.VR;

namespace LSIIC.Core
{
	public class Helpers
	{
		public static string SceneName;
		public static int SceneIndex;

		public static string H3InfoPrint(H3Info options, bool controllerDirection = true)
		{
			string ret = "";
			//0b00101111 

			if (options.HasFlag(H3Info.FPS))
				ret += "\n" + (Time.timeScale / Time.smoothDeltaTime).ToString("F0") + " FPS (" + ((1f / Time.timeScale) * Time.deltaTime * 1000).ToString("F2") + "ms) (" + Time.timeScale + "x)";
			if (options.HasFlag(H3Info.DateTime))
				ret += "\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); //ISO 8601 best girl
			if (options.HasFlag(H3Info.Transform))
				ret += "\nTransform: (0.00, 0.00, 0.00)@0°";
			if (options.HasFlag(H3Info.Health))
				ret += "\nHealth: 5000/5000 (100%)";
			if (options.HasFlag(H3Info.Scene))
				ret += "\nScene: ObjectCreation - level0";
			if (options.HasFlag(H3Info.SAUCE))
				ret += "\n123456 S.A.U.C.E.";
			if (options.HasFlag(H3Info.Headset))
				ret += "\nHeadset: Dummy Headset Model";
			if (options.HasFlag(H3Info.ControllerL))
			{
				ret += "\n" + (controllerDirection ? " Left " : "") + "Controller: " + H3InfoPrint_Controllers(3);
			}
			if (options.HasFlag(H3Info.ControllerR))
			{
				ret += "\n" + (controllerDirection ? "Right " : "") + "Controller: " + H3InfoPrint_Controllers(4);
			}

			if (ret[0] == '\n')
				ret = ret.Substring(1);

			return ret;
		}

		private static int m_maxControllerStringSize;

		public static string H3InfoPrint_Controllers(uint index)
		{
			string info = "Dummy Controller Model (100% +)";

			if (info.Length > m_maxControllerStringSize)
				m_maxControllerStringSize = info.Length;

			return info;
		}

		[Flags]
		public enum H3Info
		{
			FPS = 0x1,
			DateTime = 0x2,
			Transform = 0x4,
			Health = 0x8,
			Scene = 0x10,
			SAUCE = 0x20,
			Headset = 0x40,
			ControllerL = 0x80,
			ControllerR = 0x100,

			All = Int32.MaxValue,
			None = 0
		}

		public static string GetHeldObjects()
		{
			return "Sorry nothing";
		}

		public static string GetHeldObjects_RoundInfo(FVRFireArmRound round)
		{
			return "Sorry nothing";
		}

		public static string GetHierarchyPath(Transform obj)
		{
			string name = obj.name;
			Transform parent = obj.parent;
			while (parent != null)
			{
				name = parent.name + '/' + name;
				parent = parent.parent;
			}
			return name;
		}
	}
}