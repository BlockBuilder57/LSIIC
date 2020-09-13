using Anvil;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FistVR;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

[assembly: AssemblyVersion("0.8")]
namespace LSIIC.VirtualObjectsInjector
{
	[BepInPlugin("net.block57.lsiic.virtualobjectsinjector", "LSIIC - Virtual Objects Injector", "0.8")]
	public class VirtualObjectsInjectorPlugin : BaseUnityPlugin
	{
		public static ManualLogSource Logger { get; set; }

		private void Awake()
		{
			Logger = base.Logger;

			Logger.LogWarning("This plugin has been deprecated and has been integrated into H3VR.Sideloader.");
			Logger.LogWarning("It should only be used for hassle-free rapid prototyping from Unity into H3VR.");

			Harmony.CreateAndPatchAll(typeof(VirtualObjectsInjectorPlugin));
		}

		//injects FVRObject(s) and ItemSpawnerID(s) into IM
		[HarmonyPatch(typeof(GM), "GenerateItemDBs")]
		[HarmonyPostfix]
		public static void GM_Awake(GM __instance)
		{
			Uri StreamingAssetsUri = new Uri(Application.streamingAssetsPath + "\\dummy");
			Logger.LogDebug(Application.streamingAssetsPath + "\\dummy");
			foreach (string file in Directory.GetFiles(Paths.GameRootPath + @"\VirtualObjects", "*", SearchOption.AllDirectories))
			{
				if (Path.GetFileName(file) != Path.GetFileNameWithoutExtension(file) || Path.GetFileName(file).Contains("VirtualObjects"))
					continue;

				string relativeFile = StreamingAssetsUri.MakeRelativeUri(new Uri(file)).ToString();

				AnvilCallback<AssetBundle> bundle = AnvilManager.GetBundleAsync(relativeFile);
				bundle.AddCallback(delegate
				{
					if (bundle.Result != null)
					{
						Logger.Log(LogLevel.Info, $"Injecting FVRObject(s) and ItemSpawnerID(s) from {Path.GetFileName(file)}");

						foreach (FVRObject fvrObj in bundle.Result.LoadAllAssets<FVRObject>())
						{
							//allows the original bundle path to be ignored, a bit cursed
							FieldInfo anvilPrefab = AccessTools.Field(typeof(FVRObject), "m_anvilPrefab");
							AssetID original = (AssetID)anvilPrefab.GetValue(fvrObj);
							original.Bundle = relativeFile;
							anvilPrefab.SetValue(fvrObj, original);

							IM.OD.Add(fvrObj.ItemID, fvrObj);

							ManagerSingleton<IM>.Instance.odicTagCategory.AddOrCreate(fvrObj.Category).Add(fvrObj);
							ManagerSingleton<IM>.Instance.odicTagFirearmEra.AddOrCreate(fvrObj.TagEra).Add(fvrObj);
							ManagerSingleton<IM>.Instance.odicTagFirearmSize.AddOrCreate(fvrObj.TagFirearmSize).Add(fvrObj);
							ManagerSingleton<IM>.Instance.odicTagFirearmAction.AddOrCreate(fvrObj.TagFirearmAction).Add(fvrObj);
							ManagerSingleton<IM>.Instance.odicTagFirearmFiringMode.AddOrCreate(fvrObj.TagFirearmFiringModes.FirstOrDefault()).Add(fvrObj);
							ManagerSingleton<IM>.Instance.odicTagFirearmFeedOption.AddOrCreate(fvrObj.TagFirearmFeedOption.FirstOrDefault()).Add(fvrObj);
							ManagerSingleton<IM>.Instance.odicTagFirearmMount.AddOrCreate(fvrObj.TagFirearmMounts.FirstOrDefault()).Add(fvrObj);
							ManagerSingleton<IM>.Instance.odicTagAttachmentMount.AddOrCreate(fvrObj.TagAttachmentMount).Add(fvrObj);
							ManagerSingleton<IM>.Instance.odicTagAttachmentFeature.AddOrCreate(fvrObj.TagAttachmentFeature).Add(fvrObj);
						}
						foreach (ItemSpawnerID id in bundle.Result.LoadAllAssets<ItemSpawnerID>())
						{
							IM.CD[id.Category].Add(id);
							IM.SCD[id.SubCategory].Add(id);
						}
					}
					else
						Logger.LogError("AssetBundle is somehow null, what did you do?");
				});
			}
		}
	}

	public static class DictionaryExtension
	{
		public static TValue AddOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
		{
			if (!dictionary.ContainsKey(key))
				dictionary.Add(key, new TValue());
			return dictionary[key];
		}
	}
}
