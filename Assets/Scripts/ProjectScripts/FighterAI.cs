using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FighterAI : IController
{
	Fighter fighter;
	Stamina stamina;
	Attack nextAttack;
	
	void Awake ()
	{
		fighter = gameObject.GetComponent<Fighter> ();
		stamina = gameObject.GetComponent<Stamina> ();
	}
	
	/*
	 * Return whether or not the assigned target is now in range of an attack.
	 */
	bool IsTargetInRange (Attack attack)
	{
		Transform target = fighter.GetTarget ();
		// TODO: Check if target is in range for a specified attack
		return Vector3.Distance (transform.position, target.position) <= attack.range;
	}
	
	/*
	 * Decide what the character should do.
	 */
	public override void Think ()
	{
		FindTarget ();
		Transform fighterTarget = fighter.GetTarget ();
		// Fall back to patrol if we have no target.
		if (fighterTarget == null) {
			return;
		}

		if (fighter.IsAttacking ()) {
			return;
		}

		if (nextAttack == null) {
			nextAttack = GetNextAttack ();
		}
		// Use the attack, or else walk up to the target if not in range
		// If enemy doesn't have half it's stamina, it will back up.
		if (nextAttack != null && IsTargetInRange (nextAttack)) {
			fighter.SwingWeapon (nextAttack);
			nextAttack = null;
		} else if (stamina.HasAnyStamina ()){
			Vector3 moveDirection = fighterTarget.position - transform.position;
			fighter.Run (moveDirection);
		} else {
			Vector3 moveDirection = transform.position - fighterTarget.position;
			fighter.Run (moveDirection);
		}
	}

	/*
	 * Determine which attack to use and return it.
	 */
	Attack GetNextAttack ()
	{
		Attack[] availableAttacks = fighter.GetAttacks ();
		int rand = Random.Range (0, availableAttacks.Length);
		return availableAttacks [rand];
	}

	/*
	 * For now, finding a target is as simple as setting it to the only player
	 * in the game.
	 */
	void FindTarget ()
	{
		if (fighter.GetTarget () == null) {
			GameObject player = GameObject.FindGameObjectWithTag (Tags.PLAYER);
			if (player != null) {
				fighter.LockOnTarget (player.transform);
			}
		}
	}
}
