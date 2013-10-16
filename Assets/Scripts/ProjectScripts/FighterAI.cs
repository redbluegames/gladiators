﻿using UnityEngine;
using System.Collections;

public class FighterAI : MonoBehaviour {
	
	Fighter character;
	public Transform target;
	
	void Awake () {
		character = gameObject.GetComponent<Fighter> ();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		Think ();
	}
	
	/*
	 * Return whether or not the assigned target is now in range of an attack.
	 */
	bool targetInRange () {
		return Vector3.Distance(transform.position, target.position) <= character.swingRange;
	}
	
	/*
	 * Decide what the character should do.
	 */
	void Think () {
		FindTarget ();
		// Fall back to patrol if we have no target.
		if (target == null) {
			return;
		}
		// Approach the target if necessary
		character.TryLookAt (target.position);
		if (!targetInRange ()) {
			Vector3 moveDirection = target.position - transform.position;
			character.TryWalk (moveDirection);
		}
		// Attack if in range
		else {
			character.TrySwingWeapon ();
		}
	}
	
	/*
	 * For now, finding a target is as simple as setting it to the only player
	 * in the game.
	 */
	void FindTarget () {
		if (target == null) {
			target = GameObject.FindGameObjectWithTag (Tags.PLAYER).transform;
		}
	}
}
