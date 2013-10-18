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

	public AnimationClip swing;
	public AnimationClip idle;
	public AnimationClip windUp;
	public AnimationClip windDown;

	public Color nativeColor;

	// Character state
	enum CharacterState
	{
		Idle = 0,
		Attacking
	}
	CharacterState _characterState;

	// Store the stage of the attack
	enum AttackState
	{
		None = 0,
		WindUp,
		Swing,
		WindDown
	}
	AttackState _attackState;

	Vector3 moveDirection;
	float gravity = -20.0f;
	float verticalSpeed = 0.0f;
	float damping = 10.0f;
	CollisionFlags collisionFlags;
	
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

		// Animation sector
		if(_characterState == CharacterState.Idle)
		{
			ChangeColor (nativeColor);
			animation.Play (idle.name, PlayMode.StopAll);
		}
		else if (_characterState == CharacterState.Attacking)
		{
			if(_attackState == AttackState.WindUp)
			{
				ChangeColor (Color.yellow);
				animation.CrossFade(windUp.name, swingWindup);
			}
			else if (_attackState == AttackState.Swing)
			{
				ChangeColor (Color.red);
				animation.Play (swing.name, PlayMode. StopAll);
			}
			else if (_attackState == AttackState.WindDown)
			{
				ChangeColor (Color.magenta);
				animation.Play (windDown.name, PlayMode.StopAll);
			}
		}
	}

	void LateUpdate ()
	{
	}

	/*
	 * Return true if the character is in any of the attack states
	 */
	bool IsAttacking()
	{
		return _characterState == CharacterState.Attacking;
	}

	void ResolveActions ()
	{
		if (IsAttacking()) {
			UpdateAttackState ();
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
	void UpdateAttackState ()
	{
		float attackCompleteTime = lastSwingTime + swingWindup + swingTime + swingWindDown;
		float swingCompleteTime = lastSwingTime + swingWindup + swingTime;
		float windupCompleteTime = lastSwingTime + swingWindup;
		if (Time.time >= attackCompleteTime) {
			_characterState = CharacterState.Idle;
			_attackState = AttackState.None;
		}
		else if (Time.time >= swingCompleteTime)
		{
			_attackState = AttackState.WindDown;
		}
		else if (Time.time >= windupCompleteTime)
		{
			_attackState = AttackState.Swing;
		}
		else
		{
			_attackState = AttackState.WindUp;
		}
	}
	
	/*
	 * Try to make the character swing its weapon. If it's in the process
	 * of swinging or swing is on cooldown, it won't do anything.
	 */
	public void SwingWeapon ()
	{
		if (!IsAttacking ()) {
			_characterState = CharacterState.Attacking;
			_attackState = AttackState.WindUp;
			lastSwingTime = Time.time;
			swingTime = swing.length;
			float WINDUP_TIME = 0.5f;
			swingWindup = WINDUP_TIME;
			float WINDDOWN_TIME = 0.2f;
			swingWindDown = WINDDOWN_TIME;
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