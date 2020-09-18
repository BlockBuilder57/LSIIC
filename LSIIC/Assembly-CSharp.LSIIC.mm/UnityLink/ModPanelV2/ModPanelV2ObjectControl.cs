using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
#if !UNITY_EDITOR && !UNITY_STANDALONE
using Valve.VR;
#endif

namespace LSIIC.ModPanel
{
	public class ModPanelV2ObjectControl : MonoBehaviour
	{
		public enum ObjectType
		{
			Int32,
			Single,
			Boolean,
			Enum,
			Method,
			Vectors,
			Message
		}
		public ObjectType Type;

		public object Instance;
		public FieldInfo Field;
		public MethodInfo Method;
		public bool UpdatesOnTick;
		public object[] MethodParameters;

		private Text m_display;
		private string m_message;

		public void Awake()
		{
			if (m_display == null)
				m_display = GetComponent<Text>();
		}

		public void Update()
		{
			if (UpdatesOnTick)
				UpdateDisplay();
		}

		public void UpdateDisplay()
		{
			if (Instance != null && m_display != null)
			{
				if (Field != null)
					m_display.text = string.Format(string.IsNullOrEmpty(m_message) ? "{1} {2}: {3}" : m_message, Field.FieldType.BaseType.Name, Field.FieldType.Name, Field.Name, Field.GetValue(Instance));
				else if (Method != null)
				{
					m_display.text = string.Format(string.IsNullOrEmpty(m_message) ? "{1} {0}.{2}(" : m_message, Method.DeclaringType.Name, Method.ReturnType.Name, Method.Name);
					if (string.IsNullOrEmpty(m_message) && !m_message.Contains("{") && !m_message.Contains("}")) //now *this* is crusty
					{
						for (int i = 0; i < Method.GetParameters().Length; i++)
						{
							ParameterInfo curParam = Method.GetParameters()[i];
							m_display.text += curParam.ParameterType.Name + ' ' + curParam.Name;
							m_display.text += i == Method.GetParameters().Length - 1 ? "" : ", ";
						}
						m_display.text += ')'; //put here in case the method has no parameters
					}

				}
				else
					m_display.text = m_message;
			}
			else if (m_display != null)
			{
				Type = ObjectType.Message;
				m_display.text = "\nInstance is null!\n";
			}
			else
				Debug.LogError("Something is really wrong considering Instance and m_display are both null");
		}

		public void InitObjectControl(object instance, FieldInfo field, MethodInfo method, string message = null, bool updateOnTick = false, object[] parameters = null)
		{
			Instance = instance;
			Field = field;
			Method = method;
			UpdatesOnTick = updateOnTick;
			m_message = message;
			MethodParameters = parameters;

			Awake();
			UpdateDisplay();
		}

		public void DeltaValue(int value)
		{
			if (Field == null)
				return;

			int oldValue = (int)Field.GetValue(Instance);
			if (Field.FieldType.IsEnum)
			{
				object enumValue = Field.GetValue(Instance);
				Type enumType = enumValue.GetType();

				Array enumValues = Enum.GetValues(enumType);
				int enumCurIndex = Array.IndexOf(enumValues, enumValue);

				Field.SetValue(Instance, enumValues.GetValue((int)Mathf.Repeat(enumCurIndex + value, enumValues.Length)));
			}
			else
				Field.SetValue(Instance, oldValue + value);
			UpdateDisplay();
		}

		public void DeltaValue(float value)
		{
			if (Field == null)
				return;

			float oldValue = (float)Field.GetValue(Instance);
			Field.SetValue(Instance, oldValue + value);
			UpdateDisplay();
		}

		public void SetValue(int value)
		{
			if (Field == null)
				return;

			Field.SetValue(Instance, Convert.ChangeType(value, Field.FieldType));
			UpdateDisplay();
		}

		public void SetValue(float value)
		{
			if (Field == null)
				return;

			Field.SetValue(Instance, value);
			UpdateDisplay();
		}

		public void SetMaxValue()
		{
			if (Field == null)
				return;

			if (Field.FieldType.GetField("MaxValue") != null)
				Field.SetValue(Instance, Field.FieldType.GetField("MaxValue").GetValue(null));

			UpdateDisplay();
		}

		public void OpenKeyboard()
		{
			if (Field == null)
				return;

#if !UNITY_EDITOR && !UNITY_STANDALONE
			SteamVR_Events.System(EVREventType.VREvent_KeyboardDone).RemoveAllListeners();
			SteamVR_Events.System(EVREventType.VREvent_KeyboardDone).Listen(delegate {
				StringBuilder stringBuilder = new StringBuilder(256);
				SteamVR.instance.overlay.GetKeyboardText(stringBuilder, 256);
				string value = stringBuilder.ToString();
				object modified = Field.GetValue(Instance);

				switch (Type)
				{
					default:
						modified = Convert.ChangeType(value, Field.FieldType);
						break;
					case ObjectType.Vectors:
						{
							value = value.Replace("(", "").Replace(")", "");

							string[] inputNumbers = value.Split(',');

							if (Field.FieldType == typeof(Vector3))
							{
								modified = new Vector3(
									float.Parse(inputNumbers[0]),
									float.Parse(inputNumbers[1]),
									float.Parse(inputNumbers[2])
								);
							}
							else
							{
								modified = new Vector2(
									float.Parse(inputNumbers[0]),
									float.Parse(inputNumbers[1])
								);
							}
							
							break;
						}
				}

				Field.SetValue(Instance, modified);
				UpdateDisplay();
			});
			SteamVR.instance.overlay.ShowKeyboard((int)EGamepadTextInputMode.k_EGamepadTextInputModeNormal, (int)EGamepadTextInputLineMode.k_EGamepadTextInputLineModeSingleLine, "Enter the name of an asset.", 256, Field.GetValue(Instance).ToString(), false, 0);
#endif
		}

		public void CallMethod()
		{
			if (Method == null)
				return;

			Method.Invoke(Instance, MethodParameters);
		}
	}
}
