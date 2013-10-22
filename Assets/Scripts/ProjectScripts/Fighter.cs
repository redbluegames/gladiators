using UnityEngine;
using System.Collections;

public class Fighter : MonoBehaviour
{
	public IController controller;
	public float movespeed; //TODO Rename me runspeed;
	public float sprintspeed;
	public Transform target;
	
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
	float lastHitTime;

	// Link to the attack spherecast object
	GameObject attackCaster;

	Color desiredColor;
	Color hitColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

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
		if (characterState == CharacterState.Idle) {
			ChangeNormalColor (nativeColor);
			animation.Play (idle.name, PlayMode.StopAll);
		} else if (characterState == CharacterState.Attacking) {
			if (attackState == AttackState.WindUp) {
				ChangeNormalColor (Color.yellow);
				animation.CrossFade (windUp.name, swingWindup);
			} else if (attackState == AttackState.Swing) {
				ChangeNormalColor (Color.red);
				animation.Play (swing.name, PlayMode. StopAll);
			} else if (attackState == AttackState.WindDown) {
				ChangeNormalColor (Color.magenta);
				animation.Play (windDown.name, PlayMode.StopAll);
			}
		}

		RenderColor ();
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
	 * Any pending actions that need to finish up go here. For example, swinging
	 * a sword starts but later ends in this method, restoring the character state to ready
	 * to swing.
	 */
	void ConsumeUnresolvedActions ()
	{
		if (IsAttacking ()) {
			UpdateAttackState ();
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
		Move (direction, movespeed);
	}
	
	/*
	 * Sprint the fighter in a given direction. If fighter is out of stamina, just run.
	 */
	public void Sprint (Vector3 direction)
	{
		Stamina stam = GetComponent<Stamina> ();
		if (stam == null) {
			Debug.LogWarning (string.Format ("Object %s tried to sprint without stamina attached.", gameObject.name));
		}
		if (stam.HasStamina ()) {
			stam.UseStamina (sprintStamPerSec * Time.deltaTime);
			Move (direction, sprintspeed);
		} else {
			Move (direction, movespeed);
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
		if (!IsAttacking ()) {
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
	 * Reads input and handles action for all debug functions
	 */
	void TryDebugs ()
	{
	}

	/*
	 * Debug method to change the fighter color.
	 */
	void ChangeNormalColor (Color color)
	{
		desiredColor = color;
	}

	void RenderColor()
	{
		Color colorToShow;
		const float timeToShowHit = 0.1f;
		if(Time.time >= timeToShowHit + lastHitTime)
		{
			colorToShow = desiredColor;
		}
		else {
			colorToShow = hitColor;
		}

		renderer.material.color = colorToShow;
	}

	public void SnapToPoint (Transform point)
	{
		myTransform.position = point.transform.position;
	}

	public void TakeHit()
	{
		lastHitTime = Time.time;
	}
}