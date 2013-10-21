using UnityEngine;
using System.Collections;

public class Stamina : MonoBehaviour {
	
	public int maxStamina = 100;
	public float curStamina;
	public float cooldown = 3.0f; // Can be private once tweaked

	float lastUsedTime;
	float regenTime = 5.0f;
	
	const int NO_STAMINA = 0;

	void Awake ()
	{
		lastUsedTime = Time.time;
		curStamina = maxStamina;
	}
	
	void Update ()
	{
		if (curStamina <= maxStamina) {
			RegenOverTime ();
		}
	}
	
	/*
	 * When using an ability that causes a one time consumption of stamina,
	 * this method should be called. This handles our timer for stamina usage
	 * and adjusts or stamina value.
	 */
	public void UseStamina (float amount)
	{
		lastUsedTime = Time.time;
		AdjustStamina (-amount);
	}

	/*
	 * When using an ability that uses stamina over time call this method
	 * to ensure stamina is adjusted and stamina cooldown is respected.
	 * This factors in frames per second, so pass in the amount of stam over time
	 * that should be consumed.
	 */
	public void UseStaminaOverTime (float amount)
	{
		lastUsedTime = Time.time;
		float adj = Mathf.Min (curStamina, (amount * Time.deltaTime));
		AdjustStamina (-adj);
	}
	
	/*
	 * Check if the entity has any amount of stamina.
	 */
	public bool HasStamina ()
	{
		return curStamina > NO_STAMINA;
	}
	
	/*
	 * Adjust object stamina by a provided amount.
	 */
	void AdjustStamina (float adj)
	{
		curStamina += adj;
		if (curStamina <= NO_STAMINA) {
			curStamina = NO_STAMINA;
		} else if (curStamina > maxStamina) {
			Debug.Log ("Stamina set above 100. Let's avoid this if user already was full stam.");
			curStamina = maxStamina;
		}
	}

	/*
	 * Regen stamina provided cooldown is up. Make sure we don't over-regen beyond
	 * the max amount. Also, ensure that we adjust for the framerate using deltaTime.
	 */
	void RegenOverTime ()
	{
		float timeSinceLastUsed = Time.time - lastUsedTime;
		if (timeSinceLastUsed >= cooldown) {
			float adj = Mathf.Min (maxStamina-curStamina, (maxStamina/regenTime * Time.deltaTime));
			if (adj > 0) {
				AdjustStamina (adj);
			}
		}
	}
}
