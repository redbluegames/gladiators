using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour
{
	public int maxHealth = 100;
	public float curHealth = 100.0f;
	const int NO_HEALTH = 0;
	
	void Die ()
	{
		// play death animation
		GameObject.Destroy (gameObject);
	}
	
	public void TakeDamage (float damage)
	{
		AdjustHealth (-damage);
	}
	
	public void Heal (float healthGain)
	{
		AdjustHealth (healthGain);
	}

	/*
	 * Adjust object health by a provided amount.
	 */
	protected void AdjustHealth (float adj)
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

	public float CalculateDisplayPercent()
	{
		return curHealth / (float) maxHealth;
	}
}
