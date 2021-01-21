using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using Valve.VR;

namespace LSIIC.ModPanel
{
	public class ModPanelV2Page_Spawner : ModPanelV2Page
	{
		[Header("Spawner Page")]
		public Image ItemImage;
		public Sprite ItemImageNoIcon;
		public Text ItemInfo;

		public Transform SpawnPos;

		public GameObject OpenInItemSpawnerButton;

		private GameObject m_currentGameObj;
		private FVRObject m_currentFVRObj;
		private FVRPhysicalObject m_currentFVRPhysObj;
		private ItemSpawnerID m_currentItemSpawnerID;

		private FVRObject.ObjectCategory m_objCategory = FVRObject.ObjectCategory.Uncategorized;

		private ObjectTable m_objectTable;
		private ObjectTableDef m_objectTableDef;

		public override void PageInit()
		{
			base.PageInit();
			if (ItemInfo != null)
				ItemInfo.text = "";

			AddObjectControl(ObjectControlStart, 0, this, "m_objCategory");
		}

		public GameObject UpdateCurrentGameObj(GameObject obj)
		{
			if (obj != null)
			{
				m_currentGameObj = obj;
				m_currentFVRObj = null;
				m_currentFVRPhysObj = null;
				m_currentItemSpawnerID = null;

				if (m_currentGameObj.GetComponentInChildren<FVRPhysicalObject>() != null)
				{
					m_currentFVRPhysObj = m_currentGameObj.GetComponentInChildren<FVRPhysicalObject>();
					m_currentFVRObj = m_currentFVRPhysObj.ObjectWrapper;
					m_currentItemSpawnerID = m_currentFVRPhysObj.IDSpawnedFrom;

#if !UNITY_EDITOR && !UNITY_STANDALONE
					if (m_currentFVRObj != null && IM.HasSpawnedID(m_currentFVRObj.SpawnedFromId))
						m_currentItemSpawnerID = IM.GetSpawnerID(m_currentFVRObj.SpawnedFromId);
#endif

				}
				SpawnerRefreshElements();
			}

			return m_currentGameObj;
		}

		public GameObject UpdateCurrentGameObj(FVRObject fvrObj)
		{
			if (fvrObj != null && fvrObj.GetGameObject() != null)
				return UpdateCurrentGameObj(fvrObj.GetGameObject());
			return null;
		}

		public void SpawnObject(bool duplicate)
		{
			if (m_currentGameObj != null)
			{
				GameObject tospawn = m_currentGameObj;
				if (m_currentFVRObj != null && !duplicate)
					tospawn = m_currentFVRObj.GetGameObject();

				Instantiate(tospawn, SpawnPos.position, SpawnPos.rotation);
			}
		}

		public void DeleteObjectFromHand()
		{
			foreach (FVRViveHand hand in GM.CurrentMovementManager.Hands)
			{
				if (hand.CurrentInteractable != null)
				{
					FVRInteractiveObject FVRIntObj = hand.CurrentInteractable;
					FVRIntObj.ForceBreakInteraction();
					Destroy(FVRIntObj.gameObject);
				}
			}
		}

		public void GetObjectFromHand()
		{
			foreach (FVRViveHand hand in GM.CurrentMovementManager.Hands)
				if (hand.CurrentInteractable != null)
					UpdateCurrentGameObj(hand.CurrentInteractable.gameObject);

			SpawnerRefreshElements();
		}
		
		public void GetObjectWithKeyboard()
		{
#if !UNITY_EDITOR && !UNITY_STANDALONE
			SteamVR_Events.System(EVREventType.VREvent_KeyboardDone).RemoveAllListeners();
			SteamVR_Events.System(EVREventType.VREvent_KeyboardDone).Listen(delegate {
				StringBuilder stringBuilder = new StringBuilder(256);
				SteamVR.instance.overlay.GetKeyboardText(stringBuilder, 256);
				string assetName = stringBuilder.ToString();

				List<AssetBundle> assetBundles = new List<AssetBundle>();
				foreach (string assBundle in System.IO.Directory.GetFiles(Application.streamingAssetsPath))
					if (!assBundle.Contains("."))
						assetBundles.Add(AnvilManager.GetBundleAsync(System.IO.Path.GetFileName(assBundle)).Result);

				foreach (AssetBundle assetBundle in assetBundles)
				{
					if (assetBundle != null)
					{
						string firstMatch = assetBundle.GetAllAssetNames().FirstOrDefault(s => s.ToLower().Contains(assetName.ToLower()));

						if (firstMatch != null && !string.IsNullOrEmpty(firstMatch))
						{
							UpdateCurrentGameObj(assetBundle.LoadAsset<GameObject>(firstMatch));
							SpawnerRefreshElements(firstMatch);
							break;
						}
					}
				}
			});
			SteamVR.instance.overlay.ShowKeyboard((int)EGamepadTextInputMode.k_EGamepadTextInputModeNormal, (int)EGamepadTextInputLineMode.k_EGamepadTextInputLineModeSingleLine, "Enter the name of an asset.", 256, "", false, 0);
#endif
		}

		public void CopyFromItemSpawner()
		{
#if !UNITY_EDITOR && !UNITY_STANDALONE
			foreach (ItemSpawnerUI spawner in FindObjectsOfType<ItemSpawnerUI>())
			{
				ItemSpawnerID id = (ItemSpawnerID)AccessTools.Field(typeof(ItemSpawnerUI), "m_curID").GetValue(spawner);
				if (id != null && id.MainObject != null)
					UpdateCurrentGameObj(id.MainObject);
			}
#endif
		}

		public void OpenInItemSpawner()
		{
			if (m_currentItemSpawnerID != null && m_currentItemSpawnerID.SubCategory != ItemSpawnerID.ESubCategory.None)
			{
				foreach (ItemSpawnerUI spawner in FindObjectsOfType<ItemSpawnerUI>())
				{
					AccessTools.Field(typeof(ItemSpawnerUI), "m_curCategory").SetValue(spawner, m_currentItemSpawnerID.Category);
					AccessTools.Field(typeof(ItemSpawnerUI), "m_curSubCategory").SetValue(spawner, m_currentItemSpawnerID.SubCategory);
					AccessTools.Field(typeof(ItemSpawnerUI), "m_curID").SetValue(spawner, m_currentItemSpawnerID);
					AccessTools.Field(typeof(ItemSpawnerUI), "m_IDSelectedForSpawn").SetValue(spawner, m_currentItemSpawnerID);
					AccessTools.Method(typeof(ItemSpawnerUI), "SetMode_Details").Invoke(spawner, null);
				}
			}
#if UNITY_EDITOR || UNITY_STANDALONE
			if (m_currentItemSpawnerID != null)
				Debug.Log("ItemSpawnerUIs would open " + m_currentItemSpawnerID.DisplayName);
#endif
		}

		private void SpawnerRefreshElements(string firstMatch = "")
		{
#if !UNITY_EDITOR && !UNITY_STANDALONE
			if (ItemInfo == null || ItemImage == null)
				return;

			if (m_currentFVRObj != null)
			{
				string output = "";
				output += $"{m_currentFVRObj.DisplayName} ({m_currentFVRObj.ItemID}) - {m_currentFVRObj.Category}, {m_currentFVRObj.TagEra}, {m_currentFVRObj.TagSet}";
				output += $"\nSpawned From: {m_currentFVRObj.SpawnedFromId}";
				output += $"\nMass: {m_currentFVRObj.Mass}, Mag Cap: {m_currentFVRObj.MagazineCapacity}, Requires Picatinny Sight: {m_currentFVRObj.RequiresPicatinnySight}";
				output += $"\n   Firearm Tags: {m_currentFVRObj.TagFirearmSize}, {m_currentFVRObj.TagFirearmAction}, {m_currentFVRObj.TagFirearmRoundPower}";
				output += $"\n       Firing Modes - ";
				if (m_currentFVRObj.TagFirearmFiringModes.Count > 0)
					output += string.Join(", ", m_currentFVRObj.TagFirearmFiringModes.ConvertAll(x => x.ToString()).ToArray());
				output += $"\n       Feed Options - ";
				if (m_currentFVRObj.TagFirearmFeedOption.Count > 0)
					output += string.Join(", ", m_currentFVRObj.TagFirearmFeedOption.ConvertAll(x => x.ToString()).ToArray());
				output += $"\n             Mounts - ";
				if (m_currentFVRObj.TagFirearmMounts.Count > 0)
					output += string.Join(", ", m_currentFVRObj.TagFirearmMounts.ConvertAll(x => x.ToString()).ToArray());
				output += $"\nAttachment Tags: {m_currentFVRObj.TagAttachmentMount}, {m_currentFVRObj.TagAttachmentFeature}";
				output += $"\n     Melee Tags: {m_currentFVRObj.TagMeleeStyle}, {m_currentFVRObj.TagMeleeHandedness}";
				output += $"\n   Powerup Tags: {m_currentFVRObj.TagPowerupType}";
				output += $"\n    Thrown Tags: {m_currentFVRObj.TagThrownType}, {m_currentFVRObj.TagThrownDamageType}";

				if (m_currentItemSpawnerID != null)
				{
					ItemSpawnerCategoryDefinitions.Category Cat = Array.Find(ManagerSingleton<IM>.Instance.CatDefs.Categories, x => x.Cat == m_currentItemSpawnerID.Category);
					ItemSpawnerCategoryDefinitions.SubCategory SubCat = Array.Find(Cat.Subcats, x => x.Subcat == m_currentItemSpawnerID.SubCategory);

					output += $"\n\n";
					if (Cat != null)
						output += $"{Cat.DisplayName} ({Cat.DoesDisplay_Sandbox}, {Cat.DoesDisplay_Unlocks}) | ";
					if (SubCat != null)
						output += $"{SubCat.DisplayName} ({SubCat.DoesDisplay_Sandbox}, {SubCat.DoesDisplay_Unlocks}) | ";
					output += $"{m_currentItemSpawnerID.DisplayName} ({m_currentItemSpawnerID.ItemID})";
					output += $"\nSpawns: {m_currentItemSpawnerID.MainObject.DisplayName} on {(!(m_currentItemSpawnerID.UsesLargeSpawnPad || m_currentItemSpawnerID.UsesHugeSpawnPad) ? "Small" : (m_currentItemSpawnerID.UsesLargeSpawnPad ? "Large" : "Huge"))}";
					if (m_currentItemSpawnerID.SecondObject != null)
						output += $", {m_currentItemSpawnerID.SecondObject.DisplayName}";
					output += $"\nUnlock Cost: {m_currentItemSpawnerID.UnlockCost} S.A.U.C.E. {(m_currentItemSpawnerID.IsUnlockedByDefault ? "(Unlocked by default) -" : "-")} Is Reward: {m_currentItemSpawnerID.IsReward}";
					output += $"\nSubheading: {m_currentItemSpawnerID.SubHeading}\nDescription: {m_currentItemSpawnerID.Description}";
					ItemImage.sprite = m_currentItemSpawnerID.Sprite;
				}
				else
				{
					output += $"\n\nNo ItemSpawnerID found.";
					ItemImage.sprite = ItemImageNoIcon;
				}

				ItemInfo.text = output;
			}
			else
			{
				ItemImage.sprite = ItemImageNoIcon;
				ItemInfo.text = "No FVRObject found.";
				if (m_currentGameObj != null)
					ItemInfo.text += $"\nThe current object's name is {m_currentGameObj.name}.";
				if (!string.IsNullOrEmpty(firstMatch))
					ItemInfo.text += $"\nThe match text was {firstMatch}.";
			}

			if (OpenInItemSpawnerButton != null)
				OpenInItemSpawnerButton.SetActive(m_currentItemSpawnerID != null);
#endif
		}

		#region Get Random FVRObjects
		public void GetRandomFVRObjectFromCategory(FVRObject.ObjectCategory category)
		{
			if (m_objectTable == null)
			{
				m_objectTable = new ObjectTable();
				m_objectTableDef = (ObjectTableDef)ScriptableObject.CreateInstance(typeof(ObjectTableDef));
			}

			m_objectTable.Initialize(m_objectTableDef, category);
			UpdateCurrentGameObj(m_objectTable.GetRandomObject());
		}

		public void GetRandomFVRObject()
		{
			GetRandomFVRObjectFromCategory(m_objCategory);
		}

		public void GetRandomBespokeAttachment()
		{
			if (m_objectTable == null)
			{
				m_objectTable = new ObjectTable();
				m_objectTableDef = (ObjectTableDef)ScriptableObject.CreateInstance(typeof(ObjectTableDef));
			}

			foreach (FVRViveHand hand in GM.CurrentMovementManager.Hands)
			{
				if (hand.CurrentInteractable != null && hand.CurrentInteractable is FVRPhysicalObject)
					UpdateCurrentGameObj(m_objectTable.GetRandomBespokeAttachment((hand.CurrentInteractable as FVRPhysicalObject).ObjectWrapper));
			}
		}

		public void GetRandomAmmoObject()
		{
			if (m_objectTable == null)
			{
				m_objectTable = new ObjectTable();
				m_objectTableDef = (ObjectTableDef)ScriptableObject.CreateInstance(typeof(ObjectTableDef));
			}

			foreach (FVRViveHand hand in GM.CurrentMovementManager.Hands)
			{
				if (hand.CurrentInteractable != null && hand.CurrentInteractable is FVRPhysicalObject && m_currentFVRObj != null)
					UpdateCurrentGameObj(m_currentFVRObj.GetRandomAmmoObject((hand.CurrentInteractable as FVRPhysicalObject).ObjectWrapper));
			}
		}
		#endregion

		public void BURNINGSUPERDEATHSWORD()
		{
#if !UNITY_EDITOR && !UNITY_STANDALONE
			if (Resources.FindObjectsOfTypeAll<FlamingSwordFire>().Length > 0)
				UpdateCurrentGameObj(Resources.FindObjectsOfTypeAll<FlamingSwordFire>()[0].transform.root.gameObject);
			SpawnerRefreshElements("BURNING SUPER DEATH SWORD");
#endif
		}
	}
}
