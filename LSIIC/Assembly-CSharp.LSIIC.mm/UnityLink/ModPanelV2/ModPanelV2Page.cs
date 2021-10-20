#if !UNITY_EDITOR && !UNITY_STANDALONE
using Steamworks;
#endif
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using FistVR;

namespace LSIIC.ModPanel
{
	public class ModPanelV2Page : MonoBehaviour
	{
		public string PageTitle;
		public Vector2 ObjectControlStart = new Vector2(20, -16);
		public static float ObjectControlSpacing = -14f;

		//private bool m_hasInitialized;

		[HideInInspector]
		public List<ModPanelV2ObjectControl> ObjectControls = new List<ModPanelV2ObjectControl>();
		[HideInInspector]
		public List<ModPanelV2ObjectControl> UpdatingObjectControls = new List<ModPanelV2ObjectControl>();
		[HideInInspector]
		public List<ModPanelV2ObjectControl> SavedObjectControls = new List<ModPanelV2ObjectControl>();

		protected Dictionary<string, GameObject> Elements = new Dictionary<string, GameObject>();

		[HideInInspector]
		public ModPanelV2 Panel;

		public virtual void PageInit()
		{
			foreach (Transform child in transform)
			{
				Elements.Add(child.gameObject.name, child.gameObject);
			}

			//m_hasInitialized = true;
		}

		public virtual void PageOpen()
		{

		}

		public virtual void PageTick()
		{

		}

		public virtual void PageClose(bool destroy = false)
		{

		}

		public void OnDestroy()
		{
			PageClose(true);
		}

		/// <param name="message"><br><b>For reference, fields are formatted like so:</b></br>
		/// <br>{0} - Field.FieldType.BaseType.Name</br>
		/// <br>{1} - Field.FieldType.Name</br>
		/// <br>{2} - Field.Name</br>
		/// <br>{3} - Field.GetValue(Instance)</br>
		/// <br></br>
		/// <br><b>And methods are formatted like this:</b></br>
		/// <br>{0} - Method.DeclaringType.Name</br>
		/// <br>{1} - Method.ReturnType.Name</br>
		/// <br>{2} - Method.Name</br>
		/// <br>{3} - Parameters in "{curParam.ParameterType.Name} {curParam.Name}" format</br>
		/// </param>
		public ModPanelV2ObjectControl AddObjectControl(Vector2 startOffset, int startIndex, object instance, string memberName, string message = null, bool updateOnTick = false, bool isMethod = false, object[] methodParameters = null)
		{
			if (Panel != null)
			{
				FieldInfo Field = null;
				MethodInfo Method = null;
				ModPanelV2ObjectControl.ObjectType ObjectType = ModPanelV2ObjectControl.ObjectType.Message;

				if (!string.IsNullOrEmpty(memberName))
				{
					if (instance != null)
					{
						if (!isMethod)
							Field = AccessTools.Field(instance.GetType(), memberName);
						else
						{
							Type[] paramTypes;
							if (methodParameters != null)
							{
								paramTypes = new Type[methodParameters.Length];
								for (int i = 0; i < methodParameters.Length; i++)
									paramTypes[i] = methodParameters[i].GetType();
							}

							Method = AccessTools.Method(instance.GetType(), memberName);
						}
					}

					if (Field != null)
					{
						if (Field.FieldType.IsEnum)
							ObjectType = ModPanelV2ObjectControl.ObjectType.Enum;
						else if (Field.FieldType == typeof(Vector2) || Field.FieldType == typeof(Vector3))
							ObjectType = ModPanelV2ObjectControl.ObjectType.Vectors;
						else
							ObjectType = (ModPanelV2ObjectControl.ObjectType)Enum.Parse(typeof(ModPanelV2ObjectControl.ObjectType), Field.FieldType.Name);
					}
					else if (Method != null)
						ObjectType = ModPanelV2ObjectControl.ObjectType.Method;
					else
					{
						//Debug.Log("uh oh worm?");
						ObjectType = ModPanelV2ObjectControl.ObjectType.Message;
						message = "No member of the instance was found with the name\n" + memberName;
						message += isMethod ? "\nIt was said to be a method." : "\nIt was said to be a field.";
					}
				}
				//else if (string.IsNullOrEmpty(memberName))
				//	ObjectType = ModPanelV2ObjectControl.ObjectType.Message;
				

				ModPanelV2ObjectControl oc = Instantiate(Panel.ControlPrefabs[(int)ObjectType], this.transform).GetComponent<ModPanelV2ObjectControl>();
				oc.transform.localPosition = startOffset + new Vector2(0f, ObjectControlSpacing * startIndex);
				oc.gameObject.name += memberName;

				oc.InitObjectControl(instance, Field, Method, message, updateOnTick, methodParameters);
				ObjectControls.Add(oc);
				if (updateOnTick)
					UpdatingObjectControls.Add(oc);
				return oc;
			}
			return null;
		}

		/// <param name="messages"><br><b>For reference, fields are formatted like so:</b></br>
		/// <br>{0} - Field.FieldType.BaseType.Name</br>
		/// <br>{1} - Field.FieldType.Name</br>
		/// <br>{2} - Field.Name</br>
		/// <br>{3} - Field.GetValue(Instance)</br>
		/// <br></br>
		/// <br><b>And methods are formatted like this:</b></br>
		/// <br>{0} - Method.DeclaringType.Name</br>
		/// <br>{1} - Method.ReturnType.Name</br>
		/// <br>{2} - Method.Name</br>
		/// <br>{3} - Parameters in "{curParam.ParameterType.Name} {curParam.Name}" format</br>
		/// </param>
		public int AddObjectControls(Vector2 startOffset, int startIndex, object instance, string[] memberNames, string[] messages = null, ulong updatesOnTick = 0ul, ulong methods = 0ul, object[][] methodParams = null)
		{
			for (int i = 0; i < memberNames.Length; i++)
			{
				//if member name and message are both null or "", just ignore it
				if (!string.IsNullOrEmpty(memberNames[i]) || (messages != null && messages.Length > i && !string.IsNullOrEmpty(messages[i])))
				{
					string message = messages != null && messages.Length > i ? messages[i] : "";
					// these have to be written in reverse order
					// it makes the code less readable but it's way better than creating a bunch of new bool[]s every time it gets called
					bool isMethod = ((methods >> i) & 1) != 0;
					bool updates = ((updatesOnTick >> i) & 1) != 0;
					object[] parameters = methodParams != null && methodParams.Length > i ? methodParams[i] : null;
					AddObjectControl(startOffset, startIndex + i, instance, memberNames[i], message, updates, isMethod, parameters);
				}
			}

			return startIndex + memberNames.Length;
		}

		public void ClearObjectControls()
		{
			foreach (ModPanelV2ObjectControl control in ObjectControls)
			{
				if (control == null)
					continue;
				if (SavedObjectControls != null && SavedObjectControls.Contains(control))
					continue;

				Destroy(control.gameObject);
			}
			ObjectControls.Clear();

			foreach (ModPanelV2ObjectControl control in SavedObjectControls)
			{
				if (control == null)
					continue;

				ObjectControls.Add(control);
			}
		}
	}
}
