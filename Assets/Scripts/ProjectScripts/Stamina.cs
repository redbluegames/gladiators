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
		if (curStamina < maxStamina) {
			Regenerate ();
		}
	}

	/*
	 * Ensures stamina is adjusted and stamina cooldown is updated.
	 * Be sure to account for frames per second, so multiply the amount of
	 * stamina to consume by Time.deltaTime.
	 */
	public void UseStamina (float amount)
	{
		lastUsedTime = Time.time;
		float adj = Mathf.Min (curStamina, amount);
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
			Debug.LogWarning ("Stamina set above 100. Let's avoid this if user already was full stam.");
			curStamina = maxStamina;
		}
	}

	/*
	 * Regen stamina provided cooldown is up. Make sure we don't over-regen beyond
	 * the max amount. Also, ensure that we adjust for the framerate using deltaTime.
	 */
	void Regenerate ()
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
