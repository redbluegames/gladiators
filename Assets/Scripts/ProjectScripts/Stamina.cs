using UnityEngine;
using System.Collections;

public class Stamina : MonoBehaviour {
	
	public int maxStamina = 100;
	public int curStamina = 100;
	
	const int NO_STAMINA = 0;

	/*
	 * Adjust object stamina by a provided amount.
	 */
	public void AdjustStamina (int adj)
	{
		curStamina += adj;
		if (curStamina <= NO_STAMINA) {
			curStamina = NO_STAMINA;
			StopForBreather ();
		} else if (curStamina >= maxStamina) {
			Debug.LogWarning ("Stamina set above 100. Let's avoid this if user already was full stam.");
			curStamina = maxStamina;
		}
	}
	
	/*
	 * Fighter is out of stamina and needs to react like it.
	 */
	void StopForBreather () 
	{
		// play tired animation
		// tell fighter we're tired
	}
}
