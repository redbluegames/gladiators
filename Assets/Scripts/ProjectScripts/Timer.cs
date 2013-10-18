using UnityEngine;
using System.Collections;

public class Timer : MonoBehaviour
{
	public UILabel timerText;
	
	// Update is called once per frame
	void Update ()
	{
		if (timerText != null) {
			timerText.text = Arena.Instance.waveTimeLeft.ToString () + "s";
		} else {
			Debug.LogWarning ("Timer UILabel not set in editor. Attach timer to Timer script in HUD.");
		}
	}
}
