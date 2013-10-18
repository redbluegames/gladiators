using UnityEngine;
using System.Collections;

public class HealthBar : MonoBehaviour
{
	public Health health;
	public UISlider bar;
	
	// Update is called once per frame
	void Update ()
	{
		if (bar != null) {
			bar.sliderValue = (float) health.curHealth / health.maxHealth;
		} else {
			Debug.LogWarning ("Health Slider not set in editor. Attach slider to HealthBar script in HUD.");
		}
	}
}