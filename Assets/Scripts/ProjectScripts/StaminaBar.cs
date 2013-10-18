using UnityEngine;
using System.Collections;

public class StaminaBar : MonoBehaviour
{
	public Stamina stamina;
	UISlider bar;
	
	void Awake ()
	{
		bar = GetComponent<UISlider> ();
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (bar != null) {
			bar.sliderValue = (float) stamina.curStamina / stamina.maxStamina;
		}
	}
}