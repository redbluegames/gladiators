using UnityEngine;
using System.Collections;

public class Fighter : MonoBehaviour
{
	public float movespeed;
	public float sprintspeed;
	public float attackRange;
	Vector3 moveDirection;
	float gravity = -20.0f;
	float verticalSpeed = 0.0f;
	float damping = 10.0f;
	CollisionFlags collisionFlags;

	void Awake ()
	{
		moveDirection = transform.TransformDirection (Vector3.forward);
	}

	void Start ()
	{
	}

	void Update ()
	{
		ApplyGravity ();
		TryDebugs ();
	}

	void LateUpdate ()
	{
	}

	/*
	 * Sets vertical speed to the expected value based on whether or not the character is grounded.
	 */
	void ApplyGravity ()
	{
		if (IsGrounded ()) {
			verticalSpeed = 0.0f;
		} else {
			verticalSpeed += gravity * Time.deltaTime;
		}
	}

	/*
	 * Checks to see if the character is grounded by checking collision flags.
	 */
	bool IsGrounded ()
	{
		return (collisionFlags & CollisionFlags.CollidedBelow) != 0;
	}

	/*
	 * Apply movement in the Player's desired directions according to the various speed
	 * and movement variables.
	 */
	void Move (Vector3 direction, float speed)
	{
		if (direction != Vector3.zero) {
			moveDirection = Vector3.RotateTowards (moveDirection, direction, Mathf.Infinity, 1000);
			moveDirection = moveDirection.normalized;
		}

		// Get movement vector
		Vector3 movement = (moveDirection * speed) + new Vector3 (0.0f, verticalSpeed, 0.0f);
		movement *= Time.deltaTime;

		// Apply movement vector
		CharacterController biped = GetComponent<CharacterController> ();
		collisionFlags = biped.Move (movement);
	}
	
	/*
	 * Walk the fighter in a given direction.
	 */
	public void Walk (Vector3 direction)
	{
		Move (direction, movespeed);
	}
	
	/*
	 * Sprint the fighter in a given direction.
	 */
	public void Sprint (Vector3 direction)
	{
		Move (direction, sprintspeed);
	}
	
	/*
	 * Have the fighter look at a given position.
	 */
	public void LookAt (Vector3 targetPosition)
	{
		Quaternion targetRotation = Quaternion.LookRotation (targetPosition - transform.position);
		transform.rotation = Quaternion.Slerp (transform.rotation, targetRotation, Time.deltaTime * damping);
	}

	/*
	 * Reads input and handles action for all debug functions
	 */
	void TryDebugs ()
	{
	}

	public void SnapToPoint (Transform point)
	{
		transform.position = point.transform.position;
	}
}