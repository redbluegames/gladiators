using UnityEngine;
using System.Collections;

public class Fighter : MonoBehaviour
{
	public float movespeed;
	public float sprintspeed;
	public float swingRange;
	public float swingWindup;
	public float swingTime;
	public float swingWindDown;
	
	Vector3 moveDirection;
	float gravity = -20.0f;
	float verticalSpeed = 0.0f;
	float damping = 5.0f;
	CollisionFlags collisionFlags;

	// Character state
	bool isSwinging;
	
	// Timers
	float lastSwingTime;

	void Awake ()
	{
	}

	void Start ()
	{
		moveDirection = transform.TransformDirection (Vector3.forward);
		lastSwingTime = Time.time - (swingWindup + swingTime + swingWindDown);
	}

	void Update ()
	{
		ApplyGravity ();
		ResolveActions ();
		TryDebugs ();
	}

	void LateUpdate ()
	{
	}
	
	void ResolveActions ()
	{
		if (isSwinging) {
			FinishSwingIfAble ();
		}
	}
	
	void ChangeColor (Color color)
	{
		renderer.material.color = color;
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
		// Get movement vector
		Vector3 movement = (direction.normalized * speed) + new Vector3 (0.0f, verticalSpeed, 0.0f);
		movement *= Time.deltaTime;

		// Apply movement vector
		CharacterController biped = GetComponent<CharacterController> ();
		collisionFlags = biped.Move (movement);
		
		// Rotate to face the direction of movement immediately
		if (direction != Vector3.zero) {
			transform.rotation = Quaternion.Slerp (transform.rotation, 
				Quaternion.LookRotation(movement), Time.deltaTime * damping);
		}
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
	 * Check that enough time has passed after character swung to call the
	 * swing "complete". Once it is, restore the character state to normal.
	 */
	void FinishSwingIfAble ()
	{
		float swingCompleteTimeStamp = lastSwingTime + (swingWindup + swingTime + swingWindDown);
		if (Time.time >= swingCompleteTimeStamp) {
			isSwinging = false;
			ChangeColor (Color.black);
		}
	}
	
	/*
	 * Try to make the character swing its weapon. If it's in the process
	 * of swinging or swing is on cooldown, it won't do anything.
	 */
	public void SwingWeapon ()
	{
		if (!isSwinging) { 
			ChangeColor (Color.red);
			lastSwingTime = Time.time;
			isSwinging = true;
		}
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