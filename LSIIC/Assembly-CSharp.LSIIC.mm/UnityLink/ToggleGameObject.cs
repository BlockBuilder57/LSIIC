using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LSIIC
{
	public class ToggleGameObject : MonoBehaviour
	{
		public GameObject TargetObject;

		public void Toggle()
		{
			TargetObject.SetActive(!TargetObject.activeSelf);
		}
	}
}
