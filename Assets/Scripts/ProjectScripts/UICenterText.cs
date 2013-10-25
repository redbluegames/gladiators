using UnityEngine;
using System.Collections;

public class UICenterText : MonoBehaviour
{
	public UILabel centerText;
	float displayDuration;
	float displayedOnTime;
	
	void Update ()
	{
		if (Time.time - displayedOnTime >= displayDuration) {
			centerText.text = string.Empty;
		}
	}
	
	/*
	 * Set the centered text to a value for a provided duration.
	 */
	public void DisplayMessage (string message, float duration)
	{
		displayedOnTime = Time.time;
		displayDuration = duration;
		centerText.text = message;
	}
}
