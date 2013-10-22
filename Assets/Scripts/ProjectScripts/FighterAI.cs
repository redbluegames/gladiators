using UnityEngine;
using System.Collections;

public class FighterAI : IController
{
	Fighter fighter;
	int curTarget;
	
	void Awake ()
	{
		fighter = gameObject.GetComponent<Fighter> ();
		curTarget = 0;
	}
	
	/*
	 * Return whether or not the assigned target is now in range of an attack.
	 */
	bool targetInRange ()
	{
		Transform target = fighter.GetTarget ();
		return Vector3.Distance (transform.position, target.position) <= fighter.swingRange;
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
		
		// Attack if in range, otherwise walk to them
		if (targetInRange ()) {
			fighter.SwingWeapon ();
		} else {
			Vector3 moveDirection = fighterTarget.position - transform.position;
			fighter.Run (moveDirection);
		}
	}

	/*
	 * For now, finding a target is as simple as setting it to the only player
	 * in the game.
	 */
	void FindTarget ()
	{
		if (fighter.GetTarget () == null) {
			fighter.LockOnTarget (GameObject.FindGameObjectWithTag (Tags.PLAYER).transform);
		}
	}
}
