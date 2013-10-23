using UnityEngine;
using System;
using System.Collections;

public class Fighter : MonoBehaviour
{
	public IController controller;
	public float movespeed; //TODO Rename me runspeed;
	public float sprintspeed;
	public Transform target;
	public Stamina stamina;
	public Health health;
	
	// Animations
	public AnimationClip idle;

	// TODO: Create Scriptable objects for these attacks
	public float attackWeak_Range;
	public float attackWeak_WindupTime;
	public float attackWeak_WinddownTime;
	public AnimationClip attackWeak_Swing;
	public AnimationClip attackWeak_Windup;
	public AnimationClip attackWeak_Winddown;
	public int attackWeak_Damage;
	public float attackStrong_Range;
	public float attackStrong_WindupTime;
	public float attackStrong_WinddownTime;
	public AnimationClip attackStrong_Swing;
	public AnimationClip attackStrong_Windup;
	public AnimationClip attackStrong_Winddown;
	public int attackStrong_Damage;

	// Attacks
	public Attack[] attacks;
	Attack currentAttack;

	public enum AttackType
	{
		Weak = 0,
		Strong = 1
	}

	// Dodge
	float dodgeSpeed = 20.0f;
	float dodgeTime = 0.25f;
	float dodgeStamina = 20.0f;
	Vector3 dodgeDirection;
	public float sprintStamPerSec = 30.0f;

	// Store expected swing time
	float swingTime;

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

	// Character control members
	Vector3 moveDirection;
	float gravity = -20.0f;
	float verticalSpeed = 0.0f;
	float damping = 10.0f;
	CollisionFlags collisionFlags;

	// Timers
	float lastSwingTime = Mathf.NegativeInfinity;
	float lastHitTime = Mathf.NegativeInfinity;
	float lastDodgeTime = Mathf.NegativeInfinity;

	// Link to the attack spherecast object
	GameObject attackCaster;

	// Color management members
	Color desiredColor;
	Color hitColor = new Color (1.0f, 1.0f, 1.0f, 1.0f);
	public Color nativeColor;

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

		// Initialize attacks
		attacks = new Attack[Enum.GetNames (typeof(AttackType)).Length];
		Attack weakAttack = (Attack)ScriptableObject.CreateInstance (typeof(Attack));
		weakAttack.range = attackWeak_Range;
		weakAttack.damage = attackWeak_Damage;
		weakAttack.swing = attackWeak_Swing;
		weakAttack.windupTime = attackWeak_WindupTime;
		weakAttack.windup = attackWeak_Windup;
		weakAttack.winddown = attackWeak_Winddown;
		weakAttack.winddownTime = attackWeak_WinddownTime;
		attacks [(int)AttackType.Weak] = weakAttack;
		Attack strongAttack = (Attack)ScriptableObject.CreateInstance (typeof(Attack));
		strongAttack.range = attackStrong_Range;
		strongAttack.damage = attackStrong_Damage;
		strongAttack.swing = attackStrong_Swing;
		strongAttack.windupTime = attackStrong_WindupTime;
		strongAttack.windup = attackStrong_Windup;
		strongAttack.winddown = attackStrong_Winddown;
		strongAttack.winddownTime = attackStrong_WinddownTime;
		attacks [(int)AttackType.Strong] = strongAttack;
	}

	void Start ()
	{
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
			ChangeDesiredColor (nativeColor);
			animation.Play (idle.name, PlayMode.StopAll);
		} else if (IsAttacking ()) {
			if (attackState == AttackState.WindUp) {
				ChangeDesiredColor (Color.yellow);
				animation.CrossFade (currentAttack.windup.name, currentAttack.windupTime);
			} else if (attackState == AttackState.Swing) {
				ChangeDesiredColor (Color.red);
				animation.Play (currentAttack.swing.name, PlayMode. StopAll);
			} else if (attackState == AttackState.WindDown) {
				ChangeDesiredColor (Color.magenta);
				animation.Play (currentAttack.winddown.name, PlayMode.StopAll);
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
		
		// Rotate to face the direction of XZ movement immediately, if lockFacing isn't set
		Vector3 movementXZ = new Vector3 (movement.x, 0.0f, movement.z);
		if (target != null) {
			LockOnTarget (target);
		} else if (movementXZ != Vector3.zero) {
			myTransform.rotation = Quaternion.Slerp (myTransform.rotation,
					Quaternion.LookRotation (movementXZ), Time.deltaTime * damping);
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
			LoseTarget ();
			CheckForStamina ();
			if (stamina.HasStamina ()) {
				characterState = CharacterState.Moving;
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
		float attackCompleteTime = lastSwingTime + currentAttack.windupTime + swingTime +
			currentAttack.winddownTime;
		float swingCompleteTime = lastSwingTime + currentAttack.windupTime + swingTime;
		float windupCompleteTime = lastSwingTime + currentAttack.windupTime;
		if (Time.time >= attackCompleteTime) {
			characterState = CharacterState.Idle;
			attackState = AttackState.None;
			currentAttack = null;
		} else if (Time.time >= swingCompleteTime) {
			attackState = AttackState.WindDown;
			SetAttackActive (false);
		} else if (Time.time >= windupCompleteTime) {
			attackState = AttackState.Swing;
			SetAttackActive (true);
		} else {
			attackState = AttackState.WindUp;
		}
	}

	void SetAttackActive (bool isActive)
	{
		if (attackCaster != null) {
			//attackCaster.SetActive (isActive);
			if (isActive) {
				attackCaster.GetComponent<AttackCast> ().Begin (currentAttack);
			} else {
				attackCaster.GetComponent<AttackCast> ().End ();
			}
		}
	}
	/*
	 * Try to make the character swing its weapon. If it's in the process
	 * of swinging or swing is on cooldown, it won't do anything.
	 */
	public void SwingWeapon (AttackType attackType)
	{
		if (!IsAttacking () && !IsDodging ()) {
			characterState = CharacterState.Attacking;

			currentAttack = attacks [(int)attackType];
			attackState = AttackState.WindUp;
			lastSwingTime = Time.time;
			swingTime = currentAttack.swing.length;
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
		// Look at XZ coordinate of target only
		Vector3 lookPosition = target.position;
		lookPosition.y = myTransform.position.y;
		myTransform.LookAt (lookPosition);
	}
	
	/*
	 * Set the target transform to null, effectively losing it.
	 */
	public void LoseTarget ()
	{
		target = null;
		PlayerController player = (PlayerController)controller;
		player.ResetTargetIndex ();
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
	void ChangeDesiredColor (Color color)
	{
		desiredColor = color;
	}

	void RenderColor ()
	{
		Color colorToShow;
		const float timeToShowHit = 0.1f;
		if (Time.time >= timeToShowHit + lastHitTime) {
			colorToShow = desiredColor;
		} else {
			colorToShow = hitColor;
		}

		renderer.material.color = colorToShow;
	}

	public void SnapToPoint (Transform point)
	{
		myTransform.position = point.transform.position;
	}

	public void TakeHit (int damage)
	{
		health.AdjustHealth (damage);
		lastHitTime = Time.time;
	}

	public void NotifyAttackHit ()
	{
		GameManager.Instance.FreezeGame (0.067f);
	}
}