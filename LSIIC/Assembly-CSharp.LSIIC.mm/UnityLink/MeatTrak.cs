using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace LSIIC
{
	public class MeatTrak : MonoBehaviour
	{
		public float NumberTarget;
		public float NumberDisplay;
		public char BlankChar;
		public bool UseWideCharacters;
		public GameObject[] Displays;

		[Header("Use ASCII for these!")]
		public Sprite[] DefaultCharacters;
		public Sprite[] WideCharacters;

		private Texture2D[] m_defaultTextures;
		private Texture2D[] m_wideTextures;

		public void Awake()
		{
			m_defaultTextures = new Texture2D[DefaultCharacters.Length];
			for (int i = 0; i < DefaultCharacters.Length; i++)
				m_defaultTextures[i] = ConvertSpriteToTexture(DefaultCharacters[i]);

			m_wideTextures = new Texture2D[WideCharacters.Length];
			for (int i = 0; i < WideCharacters.Length; i++)
				m_wideTextures[i] = ConvertSpriteToTexture(WideCharacters[i]);

			NumberDisplay = NumberTarget;
			SetDisplays(NumberTarget);
		}

		public static Texture2D ConvertSpriteToTexture(Sprite sprite)
		{
			if (sprite != null)
			{
				Texture2D croppedTexture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
				var pixels = sprite.texture.GetPixels((int)sprite.textureRect.x, (int)sprite.textureRect.y, (int)sprite.textureRect.width, (int)sprite.textureRect.height);
				croppedTexture.filterMode = FilterMode.Point;
				croppedTexture.SetPixels(pixels);
				croppedTexture.Apply();
				return croppedTexture;
			}
			return null;
		}

		public void Update()
		{
			if (NumberDisplay != NumberTarget)
			{
				if (NumberDisplay < NumberTarget)
					NumberDisplay++;
				else
					NumberDisplay--;

				SetDisplays(NumberDisplay);
			}
		}

		public void SetDisplays(float number)
		{
			char[] digitChars = number.ToString().ToCharArray();
			Array.Reverse(digitChars);
			for (int i = 0; i < Displays.Length; i++)
			{
				if (i < digitChars.Length)
				{
					if (!UseWideCharacters && DefaultCharacters != null && DefaultCharacters.Length > 0)
						Displays[i].GetComponent<MeshRenderer>().material.mainTexture = m_defaultTextures[digitChars[i]];
					else if (WideCharacters != null && WideCharacters.Length > 0)
						Displays[i].GetComponent<MeshRenderer>().material.mainTexture = m_wideTextures[digitChars[i]];
				}
				else
				{
					if (!UseWideCharacters && DefaultCharacters != null && DefaultCharacters.Length > 0)
						Displays[i].GetComponent<MeshRenderer>().material.mainTexture = m_defaultTextures[BlankChar];
					else if (WideCharacters != null && WideCharacters.Length > 0)
						Displays[i].GetComponent<MeshRenderer>().material.mainTexture = m_wideTextures[BlankChar];
				}
			}
		}
	}
}
