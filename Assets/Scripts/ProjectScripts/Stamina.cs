using UnityEngine;
using System.Collections;

[RequireComponent (typeof(Fighter))]

public class Stamina : MonoBehaviour
{
	
	public int maxStamina = 100;
	public float curStamina;
	public float cooldown = 1.0f; // Can be private once tweaked
	public float regenTime = 5.0f;
	float lastUsedTime;
	Fighter myFighter;
	const int NO_STAMINA = 0;

	void Awake ()
	{
		myFighter = (Fighter)transform.root.GetComponent<Fighter> ();
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
	public bool HasAnyStamina ()
	{
		return curStamina > NO_STAMINA;
	}
	
	/*
	 * Check if the entity has at least as much stamina as the provided amount.
	 */
	public bool HasStamina (float amount)
	{
		return curStamina >= amount;
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
			float remainingStamina = maxStamina - curStamina;
			float amountToRegen = maxStamina / regenTime * Time.deltaTime;
			if (myFighter.IsBlocking) {
				float blockRechargeRate = 0.4f;
				amountToRegen *= blockRechargeRate;
			}
			float adj = Mathf.Min (remainingStamina, amountToRegen);
			if (adj > 0) {
				AdjustStamina (adj);
			}
		}
	}
}
