using UnityEngine;
using System.Collections;

public class Fighter : MonoBehaviour
{
	public IController controller;
	public float movespeed; //TODO Rename me runspeed;
	public float sprintspeed;
	public Transform target;
	public Stamina stamina;
	public Health health;
	
	// Attacks
	public float swingRange;
	public float swingWindup;
	public float swingTime;
	public float swingWindDown;
	public AnimationClip swing;
	public AnimationClip idle;
	public AnimationClip windUp;
	public AnimationClip windDown;
	public Color nativeColor;
	
	// Dodge
	float dodgeSpeed = 20.0f;
	float dodgeTime = 0.25f;
	float dodgeStamina = 20.0f;
	Vector3 dodgeDirection;
	
	// Attack attributes
	public float sprintStamPerSec = 30.0f;

	// Character state
	enum CharacterState
	{
		Idle = 0,
		Attacking,
		Moving,
		Dodging,
		Blocking
	}
	CharacterState characterState;

	// Store the stage of the attack
	enum AttackState
	{
		None = 0,
		WindUp,
		Swing,
		WindDown
	}
	AttackState attackState;

	Vector3 moveDirection;
	float gravity = -20.0f;
	float verticalSpeed = 0.0f;
	float damping = 10.0f;
	CollisionFlags collisionFlags;

	// Timers
	float lastSwingTime;
	float lastDodgeTime;

	// Link to the attack spherecast object
	GameObject attackCaster;

	// Cache our fighter's transform
	Transform myTransform;

	void Awake ()
	{
		myTransform = transform;
		// TODO make this check for controller == null, otherwise
		// it always overrides the one chosen in the editor.
		controller = GetComponent<IController> ();
		attackCaster = GameObject.Find (ObjectNames.ATTACK_CASTER);
		if (attackCaster != null) {
			attackCaster.SetActive (false);
		}
		stamina = GetComponent<Stamina> ();
		health = GetComponent<Health> ();
	}

	void Start ()
	{
		lastSwingTime = Time.time - (swingWindup + swingTime + swingWindDown);
	}

	void Update ()
	{
		ApplyGravity ();
		ConsumeUnresolvedActions ();
		UpdateLockOn ();

		controller.Think ();
		TryDebugs ();

		// Animation sector
		if (IsIdle () || IsMoving ()) {
			ChangeColor (nativeColor);
			animation.Play (idle.name, PlayMode.StopAll);
		} else if (IsAttacking ()) {
			if (attackState == AttackState.WindUp) {
				ChangeColor (Color.yellow);
				animation.CrossFade (windUp.name, swingWindup);
			} else if (attackState == AttackState.Swing) {
				ChangeColor (Color.red);
				animation.Play (swing.name, PlayMode. StopAll);
			} else if (attackState == AttackState.WindDown) {
				ChangeColor (Color.magenta);
				animation.Play (windDown.name, PlayMode.StopAll);
			}
		}
	}

	/*
	 * Ensure locked on characters always face their targets, even when the other
	 * entity moves (lock on is enforced during move as well).
	 */
	void UpdateLockOn ()
	{
		if (target != null) {
			LockOnTarget (target);
		}
	}

	/*
	 * Return true if the character is in any of the attack states
	 */
	bool IsAttacking ()
	{
		return characterState == CharacterState.Attacking;
	}
	
	/*
	 * Return true if the character is in any of the idle states.
	 */
	bool IsIdle ()
	{
		return characterState == CharacterState.Idle;
	}
	
	/*
	 * Return true if the character is in dodging state.
	 */
	bool IsDodging ()
	{
		return characterState == CharacterState.Dodging;
	}
	
	/*
	 * Return true if the character is moving (sprinting or running).
	 */
	bool IsMoving ()
	{
		return characterState == CharacterState.Moving;
	}

	/*
	 * Any pending actions that need to finish up go here. For example, swinging
	 * a sword starts but later ends in this method, restoring the character state to ready
	 * to swing.
	 */
	void ConsumeUnresolvedActions ()
	{
		if (IsAttacking ()) {
			UpdateAttackState ();
		} else if (IsDodging ()) {
			UpdateDodgeState ();
		}
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
		
		// Rotate to face the direction of movement immediately, if lockFacing isn't set
		if (direction != Vector3.zero) {
			if (target != null) {
				LockOnTarget (target);
			} else {
				myTransform.rotation = Quaternion.Slerp (myTransform.rotation, 
						Quaternion.LookRotation (movement), Time.deltaTime * damping);
			}
		}
	}
	
	/*
	 * Walk the fighter in a given direction.
	 */
	public void Run (Vector3 direction)
	{
		if (IsIdle () || IsMoving ()) {
			characterState = CharacterState.Moving;
			Move (direction, movespeed);
		}
	}
	
	/*
	 * Sprint the fighter in a given direction. If fighter is out of stamina, just run.
	 */
	public void Sprint (Vector3 direction)
	{
		if (IsIdle () || IsMoving ()) {
			characterState = CharacterState.Moving;
			CheckForStamina ();
			if (stamina.HasStamina ()) {
				stamina.UseStamina (sprintStamPerSec * Time.deltaTime);
				Move (direction, sprintspeed);
			} else {
				Move (direction, movespeed);
			}
		}
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
			characterState = CharacterState.Idle;
			attackState = AttackState.None;
		} else if (Time.time >= swingCompleteTime) {
			attackState = AttackState.WindDown;
			SetAttackActive(false);
		} else if (Time.time >= windupCompleteTime) {
			attackState = AttackState.Swing;
			SetAttackActive(true);
		} else {
			attackState = AttackState.WindUp;
		}
	}

	void SetAttackActive(bool isActive)
	{
		if(attackCaster != null)
		{
			attackCaster.SetActive (isActive);
		}
	}
	/*
	 * Try to make the character swing its weapon. If it's in the process
	 * of swinging or swing is on cooldown, it won't do anything.
	 */
	public void SwingWeapon ()
	{
		if (!IsAttacking () && !IsDodging ()) {
			characterState = CharacterState.Attacking;
			attackState = AttackState.WindUp;
			lastSwingTime = Time.time;
			swingTime = swing.length;
			float WINDUP_TIME = 0.5f;
			swingWindup = WINDUP_TIME;
			float WINDDOWN_TIME = 0.2f;
			swingWindDown = WINDDOWN_TIME;
		}
	}
	
	/*
	 * Resolve the dodge roll once it's complete.
	 */
	void UpdateDodgeState ()
	{
		if (Time.time - lastDodgeTime >= dodgeTime) {
			characterState = CharacterState.Idle;
		} else {
			Dodge (dodgeDirection);
		}
	}
	
	/*
	 * Cause fighter to dodge in a given direction. Requires stamina.
	 */
	public void Dodge (Vector3 direction)
	{
		CheckForStamina ();
		if (stamina.HasStamina () && (IsMoving () || IsIdle ())) {
			dodgeDirection = direction;
			characterState = CharacterState.Dodging;
			lastDodgeTime = Time.time;
		}
		if (IsDodging ()) {
			float dodgeStamPerSec = dodgeStamina / dodgeTime;
			stamina.UseStamina (dodgeStamPerSec * Time.deltaTime);
			Move (dodgeDirection, dodgeSpeed);
		}
	}
	
	/*
	 * Set the Fighter target to the provided Transform and start staring it down.
	 */
	public void LockOnTarget (Transform newTarget)
	{
		target = newTarget;
		myTransform.LookAt (target);
	}
	
	/*
	 * Set the target transform to null, effectively losing it.
	 */
	public void LoseTarget ()
	{
		target = null;
	}
	
	/*
	 * Return the fighter's current target Transform.
	 */
	public Transform GetTarget ()
	{
		return target;
	}
	
	/*
	 * Null-protect stamina related skills in case an entity uses an ability without
	 * attaching a stamina script.
	 */
	void CheckForStamina ()
	{
		if (stamina == null) {
			Debug.LogWarning (string.Format ("Object %s used stamina ability without attaching Stamina script.", 
				gameObject.name));
		}
	}

	/*
	 * Reads input and handles action for all debug functions
	 */
	void TryDebugs ()
	{
	}

	/*
	 * Debug method to change the fighter color.
	 */
	void ChangeColor (Color color)
	{
		renderer.material.color = color;
	}

	public void SnapToPoint (Transform point)
	{
		myTransform.position = point.transform.position;
	}
}