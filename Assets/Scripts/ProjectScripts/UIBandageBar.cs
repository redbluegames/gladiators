using UnityEngine;
using System.Collections;

public class UIBandageBar : MonoBehaviour
{
	public Bandages bandage;
	public UILabel bandagesLeftLabel;
	public UISlider bar;

	// Update is called once per frame
	void Update ()
	{
		if (bar != null) {
			if (bandage.IsApplyingTime()) {
				bar.gameObject.SetActive (true);
				bar.sliderValue = bandage.CalculateDisplayPercent ();
			} else {
				bar.gameObject.SetActive (false);
			}
			bandagesLeftLabel.text = "Bandages Left: " + bandage.bandageCount;
		} else {
			Debug.LogWarning ("Stamina Slider not set in editor. Attach slider to StaminaBar script in HUD.");
		}
	}
}