using UnityEngine;
using System.Collections;

public class FighterAI : MonoBehaviour
{
	Fighter fighter;
	public Transform target;
	
	void Awake ()
	{
		fighter = gameObject.GetComponent<Fighter> ();
	}
	
	// Update is called once per frame
	void Update ()
	{
		Think ();
	}
	
	/*
	 * Return whether or not the assigned target is now in range of an attack.
	 */
	bool targetInRange ()
	{
		return Vector3.Distance (transform.position, target.position) <= fighter.swingRange;
	}
	
	/*
	 * Decide what the character should do.
	 */
	void Think ()
	{
		FindTarget ();
		// Fall back to patrol if we have no target.
		if (target == null) {
			return;
		}

		// Always look at the target
		fighter.LookAt (target.position);

		// Attack if in range, otherwise walk to them
		if (targetInRange ()) {
			fighter.SwingWeapon ();
		} else {
			Vector3 moveDirection = target.position - transform.position;
			fighter.Walk (moveDirection);
		}
	}
	
	/*
	 * For now, finding a target is as simple as setting it to the only player
	 * in the game.
	 */
	void FindTarget ()
	{
		if (target == null) {
			target = GameObject.FindGameObjectWithTag (Tags.PLAYER).transform;
		}
	}
}
