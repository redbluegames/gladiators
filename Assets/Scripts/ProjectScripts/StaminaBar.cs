using UnityEngine;
using System.Collections;

public class StaminaBar : MonoBehaviour
{
	public Stamina stamina;
	public UISlider bar;

	// Update is called once per frame
	void Update ()
	{
		if (bar != null) {
			bar.sliderValue = (float) stamina.curStamina / stamina.maxStamina;
		} else {
			Debug.LogWarning ("Stamina Slider not set in editor. Attach slider to StaminaBar script in HUD.");
		}
	}
}