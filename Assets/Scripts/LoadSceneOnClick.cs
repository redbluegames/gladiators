using UnityEngine;
using System.Collections;

public class LoadSceneOnClick : MonoBehaviour {
	
	public string sceneName;

	void OnClick ()
	{
		if (!string.IsNullOrEmpty(sceneName))
		{
			Application.LoadLevel(sceneName);
		} else {
			Debug.LogError ("Scene name not set. Scene not loaded.");
		}
	}
}
