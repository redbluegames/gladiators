using UnityEngine;
using System.Collections;

public class UITimer : MonoBehaviour
{
	public UILabel timerText;
	public UILabel timerLabel;
	
	// Update is called once per frame
	void Update ()
	{
		// Disable the labels when arena is no longer running
		timerLabel.enabled = Arena.Instance.IsRunning;
		timerText.enabled = Arena.Instance.IsRunning;

		if (timerText != null) {
			timerText.text = ((int)Mathf.Ceil (Arena.Instance.waveTimeLeft)).ToString () + "s";
		} else {
			Debug.LogWarning ("Timer UILabel not set in editor. Attach timer to Timer script in HUD.");
		}
	}
}
