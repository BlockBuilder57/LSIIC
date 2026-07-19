using System;
using System.Reflection;
using FistVR;
using HarmonyLib;
using UnityEngine;

namespace LSIIC
{
	internal static class H3VRCurrentCompatibility
	{
		private static FieldInfo m_currentSpectatorSettings;
		private static FieldInfo m_spectatorFov;
		private static FieldInfo m_previewMode;
		private static MethodInfo m_getRandomAmmoObject;
		private static MethodInfo m_initializeObjectTable;
		private static MethodInfo m_updateSosigPlayerBodyState;

		private static bool m_warnedSpectatorOptions;
		private static bool m_warnedSpectatorFov;
		private static bool m_warnedPreviewMode;
		private static bool m_warnedRandomAmmo;
		private static bool m_warnedObjectTable;
		private static bool m_warnedPlayerSosigBody;

		public static bool TryAdjustSpectatorFov(float delta)
		{
			object currentSettings = GetSpectatorCameraSettings();
			if (currentSettings == null)
				return false;

			if (m_spectatorFov == null || m_spectatorFov.DeclaringType != currentSettings.GetType())
				m_spectatorFov = AccessTools.Field(currentSettings.GetType(), "CameraFov");
			if (m_spectatorFov == null || m_spectatorFov.FieldType != typeof(float))
			{
				WarnOnce(ref m_warnedSpectatorFov, "Spectator camera FOV field is unavailable.");
				return false;
			}

			float fov = Mathf.Clamp((float)m_spectatorFov.GetValue(currentSettings) + delta, 10f, 180f);
			m_spectatorFov.SetValue(currentSettings, fov);
			LogDebug("Spectator camera FOV set to " + fov + ".");
			return true;
		}

		public static object GetSpectatorCameraSettings()
		{
			object spectatorOptions = GetSpectatorOptions();
			if (spectatorOptions == null)
				return null;

			if (m_currentSpectatorSettings == null || m_currentSpectatorSettings.DeclaringType != spectatorOptions.GetType())
				m_currentSpectatorSettings = AccessTools.Field(spectatorOptions.GetType(), "CurrentSettings");
			object currentSettings = m_currentSpectatorSettings == null ? null : m_currentSpectatorSettings.GetValue(spectatorOptions);
			if (currentSettings == null)
				WarnOnce(ref m_warnedSpectatorFov, "Spectator camera settings are unavailable.");
			return currentSettings;
		}

		public static bool TryEnableSpectatorPreview()
		{
			object spectatorOptions = GetSpectatorOptions();
			if (spectatorOptions == null)
				return false;

			if (m_previewMode == null || m_previewMode.DeclaringType != spectatorOptions.GetType())
				m_previewMode = AccessTools.Field(spectatorOptions.GetType(), "PreviewMode");
			if (m_previewMode == null || !m_previewMode.FieldType.IsEnum)
			{
				WarnOnce(ref m_warnedPreviewMode, "Spectator preview mode field is unavailable.");
				return false;
			}

			m_previewMode.SetValue(spectatorOptions, Enum.Parse(m_previewMode.FieldType, "Enabled"));
			LogDebug("Spectator preview mode enabled.");
			return true;
		}

		public static FVRObject GetRandomAmmoObject(FVRObject firearm)
		{
			if (m_getRandomAmmoObject == null)
			{
				foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(FVRObject)))
				{
					ParameterInfo[] parameters = method.GetParameters();
					if (method.Name == "GetRandomAmmoObject" && method.IsStatic && method.ReturnType == typeof(FVRObject) && parameters.Length > 0 && parameters[0].ParameterType == typeof(FVRObject))
					{
						m_getRandomAmmoObject = method;
						break;
					}
				}
			}
			if (m_getRandomAmmoObject == null)
			{
				WarnOnce(ref m_warnedRandomAmmo, "Random ammo selection method is unavailable.");
				return null;
			}

			FVRObject ammo = m_getRandomAmmoObject.Invoke(null, GetArguments(m_getRandomAmmoObject, firearm)) as FVRObject;
			LogDebug("Random ammo selection returned " + (ammo == null ? "no item" : ammo.ItemID) + ".");
			return ammo;
		}

		public static void InitializeObjectTable(ObjectTable table, ObjectTableDef definition, FVRObject.ObjectCategory category)
		{
			if (m_initializeObjectTable == null)
			{
				foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(ObjectTable)))
				{
					ParameterInfo[] parameters = method.GetParameters();
					if (method.Name == "Initialize" && parameters.Length > 1 && parameters[0].ParameterType == typeof(ObjectTableDef) && parameters[1].ParameterType == typeof(FVRObject.ObjectCategory))
					{
						m_initializeObjectTable = method;
						break;
					}
				}
			}
			if (m_initializeObjectTable == null)
			{
				WarnOnce(ref m_warnedObjectTable, "ObjectTable initialization method is unavailable.");
				return;
			}

			m_initializeObjectTable.Invoke(table, GetArguments(m_initializeObjectTable, definition, category));
			LogDebug("ObjectTable initialized for " + category + ".");
		}

		public static void UpdateSosigPlayerBodyState(FVRPlayerBody playerBody)
		{
			if (m_updateSosigPlayerBodyState == null)
				m_updateSosigPlayerBodyState = AccessTools.Method(typeof(FVRPlayerBody), "UpdateSosigPlayerBodyState");
			if (m_updateSosigPlayerBodyState == null)
			{
				WarnOnce(ref m_warnedPlayerSosigBody, "PlayerSosigBody update method is unavailable.");
				return;
			}

			m_updateSosigPlayerBodyState.Invoke(playerBody, null);
			LogDebug("PlayerSosigBody state updated from current clothing ID.");
		}

		private static object GetSpectatorOptions()
		{
			if (GM.Options == null)
			{
				WarnOnce(ref m_warnedSpectatorOptions, "Game options are unavailable.");
				return null;
			}

			FieldInfo spectatorOptionsField = AccessTools.Field(GM.Options.GetType(), "SpectatorCamera");
			PropertyInfo spectatorOptionsProperty = AccessTools.Property(GM.Options.GetType(), "SpectatorCamera");
			object spectatorOptions = spectatorOptionsField != null
				? spectatorOptionsField.GetValue(GM.Options)
				: spectatorOptionsProperty != null ? spectatorOptionsProperty.GetValue(GM.Options, null) : null;
			if (spectatorOptions == null)
				WarnOnce(ref m_warnedSpectatorOptions, "Spectator camera options are unavailable.");
			return spectatorOptions;
		}

		private static object[] GetArguments(MethodInfo method, params object[] providedArguments)
		{
			ParameterInfo[] parameters = method.GetParameters();
			object[] arguments = new object[parameters.Length];
			for (int i = 0; i < arguments.Length; i++)
			{
				if (i < providedArguments.Length)
				{
					arguments[i] = providedArguments[i];
				}
				else if (parameters[i].DefaultValue != DBNull.Value)
				{
					arguments[i] = parameters[i].DefaultValue;
				}
				else if (parameters[i].ParameterType.IsValueType)
				{
					arguments[i] = Activator.CreateInstance(parameters[i].ParameterType);
				}
			}
			return arguments;
		}

		private static void LogDebug(string message)
		{
			Type corePlugin = AccessTools.TypeByName("LSIIC.Core.CorePlugin");
			FieldInfo debugLogging = corePlugin == null ? null : AccessTools.Field(corePlugin, "_debugCompatibilityLogging");
			object configEntry = debugLogging == null ? null : debugLogging.GetValue(null);
			PropertyInfo value = configEntry == null ? null : AccessTools.Property(configEntry.GetType(), "Value");
			object debugValue = value == null ? null : value.GetValue(configEntry, null);
			if (debugValue is bool && (bool)debugValue)
				Debug.Log("[LSIIC][Current H3VR] " + message);
		}

		private static void WarnOnce(ref bool wasWarned, string message)
		{
			if (wasWarned)
				return;

			wasWarned = true;
			Debug.LogWarning("[LSIIC][Current H3VR] " + message);
		}
	}
}
