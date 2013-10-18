using UnityEngine;
using System.Collections;

public class HealthBar : MonoBehaviour
{
	public Health health;
	UISlider bar;
	
	void Awake ()
	{
		bar = GetComponent<UISlider> ();
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (bar != null) {
			bar.sliderValue = (float) health.curHealth / health.maxHealth;
		}
	}
}