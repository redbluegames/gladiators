using UnityEngine;
using System.Collections;

public class UIHealthBar : MonoBehaviour
{
	public Health health;
	public UISlider bar;
	
	// Update is called once per frame
	void Update ()
	{
		if (bar != null) {
			bar.sliderValue = health.CalculateDisplayPercent();
		} else {
			Debug.LogWarning ("Health Slider not set in editor. Attach slider to HealthBar script in HUD.");
		}
	}
}