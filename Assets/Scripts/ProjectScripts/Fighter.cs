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
	public AnimationClip windUp;
	public AnimationClip windDown;
	public TrailRenderer swingTrail;

	// TODO: Create Scriptable objects for these attacks
	public float attackWeak_Range;
	public float attackWeak_WindupTime;
	public float attackWeak_WinddownTime;
	public AnimationClip attackWeak_Swing;
	public AnimationClip attackWeak_Windup;
	public AnimationClip attackWeak_Winddown;
	public int attackWeak_Damage;
	public Attack.ReactionType attackWeak_ReactionType;
	public float attackStrong_Range;
	public float attackStrong_WindupTime;
	public float attackStrong_WinddownTime;
	public AnimationClip attackStrong_Swing;
	public AnimationClip attackStrong_Windup;
	public AnimationClip attackStrong_Winddown;
	public int attackStrong_Damage;
	public Attack.ReactionType attackStrong_ReactinType;

	// Attacks
	public Attack[] attacks;
	public Attack currentAttack;

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
	
	// Flinch
	float flinchTime = 0.1f;
	
	// Knockback
	float knockbackMoveTime = 0.1f;
	float knockbackStunTime = 0.5f;
	
	// Store expected swing time
	float swingTime;

	// Character state
	enum CharacterState
	{
		Idle = 0,
		Attacking,
		Moving,
		Dodging,
		Blocking,
		Flinching,
		Knockedback
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
	float lastFlinchTime = Mathf.NegativeInfinity;
	float lastKnockbackTime = Mathf.NegativeInfinity;
	
	// Link to the attack spherecast object
	AttackCast attackCaster;

	// Color management members
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
		attackCaster = GetComponentInChildren<AttackCast> ();
		if (attackCaster != null) {
			attackCaster.gameObject.SetActive (false);
		}
		stamina = GetComponent<Stamina> ();
		health = GetComponent<Health> ();
		if (swingTrail == null) {
			swingTrail = GetComponentInChildren<TrailRenderer> ();
			if (swingTrail != null) {
				swingTrail.renderer.enabled = false;
			}
		}

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
		weakAttack.reactionType = attackWeak_ReactionType;
		attacks [(int)AttackType.Weak] = weakAttack;
		Attack strongAttack = (Attack)ScriptableObject.CreateInstance (typeof(Attack));
		strongAttack.range = attackStrong_Range;
		strongAttack.damage = attackStrong_Damage;
		strongAttack.swing = attackStrong_Swing;
		strongAttack.windupTime = attackStrong_WindupTime;
		strongAttack.windup = attackStrong_Windup;
		strongAttack.winddown = attackStrong_Winddown;
		strongAttack.winddownTime = attackStrong_WinddownTime;
		strongAttack.reactionType = attackStrong_ReactinType;
		attacks [(int)AttackType.Strong] = strongAttack;
	}

	void Start ()
	{
	}

	void Update ()
	{
		ApplyGravity ();
		PerformActionInProgress ();
		UpdateLockOn ();

		controller.Think ();
		TryDebugs ();

		// Animation sector
		if (IsIdle () || IsMoving () || IsFlinching () || IsKnockedBack ()) {
			// Interrupt or stop attack animation
			animation.Play (idle.name, PlayMode.StopAll);
		} else if (IsAttacking ()) {
			if (attackState == AttackState.WindUp) {
				animation.CrossFade (currentAttack.windup.name, currentAttack.windupTime);
			} else if (attackState == AttackState.Swing) {
				if (swingTrail != null) {
					swingTrail.renderer.enabled = true;
				}
				animation.Play (currentAttack.swing.name, PlayMode. StopAll);
			} else if (attackState == AttackState.WindDown) {
				if (swingTrail != null) {
					swingTrail.renderer.enabled = false;
				}
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
	public bool IsAttacking ()
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
	 * Return true if the character is flinching.
	 */
	bool IsFlinching ()
	{
		return characterState == CharacterState.Flinching;
	}
	
	/*
	 * Return true if the character is knocked back.
	 */
	bool IsKnockedBack ()
	{
		return characterState == CharacterState.Knockedback;
	}

	/*
	 * Any pending actions that need to finish up go here. For example, swinging
	 * a sword starts but later ends in this method, restoring the character state to ready
	 * to swing.
	 */
	void PerformActionInProgress ()
	{
		if (IsAttacking ()) {
			UpdateAttackState ();
		} else if (IsDodging ()) {
			UpdateDodgeState ();
		} else if (IsFlinching ()) {
			UpdateFlinchState ();
		} else if (IsKnockedBack ()) {
			UpdateKnockbackState ();
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
				attackCaster.Begin (currentAttack);
			} else {
				attackCaster.End ();
			}
		}
	}
	/*
	 * Try to make the character swing its weapon. If it's in the process
	 * of swinging or swing is on cooldown, it won't do anything.
	 */
	public void SwingWeapon (AttackType attackType)
	{
		SwingWeapon (attacks [(int)attackType]);
	}

	public void SwingWeapon(Attack attack)
	{
		if (!IsAttacking () && !IsDodging () && !IsFlinching () && !IsKnockedBack ()) {
			characterState = CharacterState.Attacking;

			currentAttack = attack;
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
	 * Use a timer to stun a character for a configurable amount of time.
	 */
	void UpdateFlinchState ()
	{
		if (Time.time - lastFlinchTime >= flinchTime) {
			characterState = CharacterState.Idle;
		}
	}
	
	/*
	 * Interrupt fighter attack and cause stun for configurable amount of time.
	 * This method only puts the fighter in the flinching state and clears any
	 * other states it might have set.
	 */
	public void Flinch ()
	{
		characterState = CharacterState.Flinching;
		attackState = AttackState.None;
		lastFlinchTime = Time.time;
	}
	
	/*
	 * Knockback consists of a pushback stage and stun stage. This method uses
	 * timers for both to determine which it is in. When both timers are up,
	 * return the fighter to Idle, allowing movement again.
	 */
	void UpdateKnockbackState ()
	{
		float timeInKnockbackState = Time.time - lastKnockbackTime;
		if (timeInKnockbackState >= knockbackMoveTime + knockbackStunTime) {
			characterState = CharacterState.Idle;
		} else if (timeInKnockbackState < knockbackMoveTime){
			Move (dodgeDirection, dodgeSpeed);
		}
	}
	
	/*
	 * Interrupt fighter, push them back, and stun them for a second. This
	 * method only puts the fighter in the knockedback state and clears any
	 * other conflicting states it might have set.
	 */
	public void Knockback (Vector3 direction)
	{
		characterState = CharacterState.Knockedback;
		attackState = AttackState.None;
		lastKnockbackTime = Time.time;
		dodgeDirection = direction;
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
	 * Return a reference to the fighter's attacks
	 */
	public Attack[] GetAttacks()
	{
		return attacks;
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
	 * Render the color of the fighter, according to the state it is in.
	 */
	void RenderColor ()
	{
		Color colorToShow;
		const float timeToShowHit = 0.1f;
		if (Time.time >= timeToShowHit + lastHitTime) {
			switch (characterState) {
			case CharacterState.Idle:
				colorToShow = nativeColor;
				break;
			case CharacterState.Attacking:
				colorToShow = Color.yellow;
				break;
			case CharacterState.Dodging:
				colorToShow = Color.green;
				break;
			case CharacterState.Flinching:
				colorToShow = Color.magenta;
				break;
			case CharacterState.Knockedback:
				colorToShow = Color.blue;
				break;
			case CharacterState.Moving:
				colorToShow = nativeColor;
				break;
			default:
				colorToShow = nativeColor;
				break;
			}
		} else {
			colorToShow = hitColor;
		}

		renderer.material.color = colorToShow;
	}

	public void SnapToPoint (Transform point)
	{
		myTransform.position = point.transform.position;
	}
	
	/*
	 * Assign damage and perform the appropriate reaction.
	 */
	public void TakeHit (Attack attack, Transform attacker)
	{
		health.AdjustHealth (attack.damage);
		lastHitTime = Time.time;
		if (attack.reactionType == Attack.ReactionType.Knockback) {
			// Knock back in the opposite direction of the attacker.
			Knockback ((myTransform.position - attacker.position).normalized);
		} else if (attack.reactionType == Attack.ReactionType.Flinch) {
			Flinch ();
		}
	}

	public void NotifyAttackHit ()
	{
		//GameManager.Instance.FreezeGame (0.067f);
	}
}