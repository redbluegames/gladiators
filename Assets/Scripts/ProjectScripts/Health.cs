using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour {
	
	public int maxHealth = 100;
	public int curHealth = 100;
	
	const int NO_HEALTH = 0;
	
	/*
	 * Adjust object health by a provided amount.
	 */
	public void AdjustHealth (int adj)
	{
		curHealth += adj;
		if (curHealth <= NO_HEALTH) {
			curHealth = NO_HEALTH;
			Die ();
		} else if (curHealth >= maxHealth) {
			Debug.LogWarning ("HP set above 100. Let's avoid this if user already was full hp.");
			curHealth = maxHealth;
		}
	}
	
	void Die () 
	{
		// play death animation
		GameObject.Destroy (gameObject);
	}
}
