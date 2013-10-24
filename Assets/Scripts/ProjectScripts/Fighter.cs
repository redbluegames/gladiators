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
	public AnimationClip attackIdle;
	public AnimationClip attackWindUp;
	public AnimationClip attackWindDown;
	public AnimationClip blockIdle;
	public AnimationClip blockWindUp;
	public AnimationClip blockWindDown;
	public TrailRenderer swingTrail;

	// TODO: Create Scriptable objects for these attacks
	public float attackWeak_Range;
	public float attackWeak_WindupTime;
	public float attackWeak_WinddownTime;
	public AnimationClip attackWeak_Swing;
	public AnimationClip attackWeak_Windup;
	public AnimationClip attackWeak_Winddown;
	public int attackWeak_Damage;
	public float attackWeak_FlinchDuration;
	public float attackWeak_KnockbackDuration;
	public Attack.ReactionType attackWeak_ReactionType;
	public float attackStrong_Range;
	public float attackStrong_WindupTime;
	public float attackStrong_WinddownTime;
	public AnimationClip attackStrong_Swing;
	public AnimationClip attackStrong_Windup;
	public AnimationClip attackStrong_Winddown;
	public int attackStrong_Damage;
	public float attackStrong_FlinchDuration;
	public float attackStrong_KnockbackDuration;
	public Attack.ReactionType attackStrong_ReactionType;

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
	Vector3 currentDodgeDirection;
	public float sprintStamPerSec = 30.0f;
	
	// Flinch and Knockback
	float currentFlinchDuration = 0.1f;
	float currentMoveReactionDuration = 0.6f;
	Vector3 currentMoveReactionDirection;
	// How much of the knockback is the target moving?
	const float KNOCKBACK_MOVE_PORTION = 0.35f; 
	
	// Store expected swing time
	float swingTime;

	// Character state
	enum CharacterState
	{
		Idle = 0,
		Attacking,
		Moving,
		Dodging,
		Blocked,
		Blocking,
		BlockingFlinch,
		Flinching,
		Knockedback,
		KnockedbackByBlock
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
		weakAttack.flinchDuration = attackWeak_FlinchDuration;
		weakAttack.knockbackDuration = attackStrong_KnockbackDuration;
		attacks [(int)AttackType.Weak] = weakAttack;
		Attack strongAttack = (Attack)ScriptableObject.CreateInstance (typeof(Attack));
		strongAttack.range = attackStrong_Range;
		strongAttack.damage = attackStrong_Damage;
		strongAttack.swing = attackStrong_Swing;
		strongAttack.windupTime = attackStrong_WindupTime;
		strongAttack.windup = attackStrong_Windup;
		strongAttack.winddown = attackStrong_Winddown;
		strongAttack.winddownTime = attackStrong_WinddownTime;
		strongAttack.reactionType = attackStrong_ReactionType;
		strongAttack.flinchDuration = attackStrong_FlinchDuration;
		strongAttack.knockbackDuration = attackStrong_KnockbackDuration;
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
		if (IsFlinching () || IsKnockedBack () || IsKnockedBackByBlock()) {
			// Interrupt or stop attack animation
			animation.Play (attackIdle.name, PlayMode.StopAll);
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
				animation.PlayQueued (attackIdle.name, QueueMode.PlayNow);
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
	 * Return true if the character is in blocking state;
	 */
	public bool IsBlocking ()
	{
		return characterState == CharacterState.Blocking;
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
	 * Return true if the fighter is in the middle of a flinch
	 * caused by performing a successful block.
	 */
	bool IsBlockFlinching ()
	{
		return characterState == CharacterState.BlockingFlinch;
	}
	
	/*
	 * Return true if the character is knocked back.
	 */
	bool IsKnockedBack ()
	{
		return characterState == CharacterState.Knockedback;
	}
	
	/*
	 * Yet another state check bool...
	 */
	bool IsKnockedBackByBlock ()
	{
		return characterState == CharacterState.KnockedbackByBlock;
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
		} else if (IsKnockedBackByBlock ()) {
			UpdateKnockbackByBlockState ();
		} else if (IsBlockFlinching ()) {
			UpdateBlockingFlinchState ();
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
		if (IsIdle () || IsMoving ()) {
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
			float dodgeStamPerSec = dodgeStamina / dodgeTime;
			stamina.UseStamina (dodgeStamPerSec * Time.deltaTime);
			Move (currentDodgeDirection, dodgeSpeed);
		}
	}
	
	/*
	 * Cause fighter to dodge in a given direction. Requires stamina.
	 */
	public void Dodge (Vector3 direction)
	{
		CheckForStamina ();
		if (stamina.HasStamina () && (IsMoving () || IsIdle ())) {
			currentDodgeDirection = direction;
			characterState = CharacterState.Dodging;
			lastDodgeTime = Time.time;
		}
	}
	
	/*
	 * Use a timer to stun a character for a configurable amount of time.
	 */
	void UpdateFlinchState ()
	{
		if (Time.time - lastFlinchTime >= currentFlinchDuration) {
			characterState = CharacterState.Idle;
		}
	}
	
	/*
	 * Interrupt fighter attack and cause stun for configurable amount of time.
	 * This method only puts the fighter in the flinching state and clears any
	 * other states it might have set.
	 */
	public void ReceiveFlinch (float duration)
	{
		characterState = CharacterState.Flinching;
		attackState = AttackState.None;
		lastFlinchTime = Time.time;
		currentFlinchDuration = duration;
	}
	
	/*
	 * Knockback consists of a pushback stage and stun stage. This method uses
	 * timers for both to determine which it is in. When both timers are up,
	 * return the fighter to Idle, allowing movement again.
	 */
	void UpdateKnockbackState ()
	{
		float timeInKnockbackState = Time.time - lastKnockbackTime;
		if (timeInKnockbackState >= currentMoveReactionDuration) {
			characterState = CharacterState.Idle;
		} else if (timeInKnockbackState < (KNOCKBACK_MOVE_PORTION * currentMoveReactionDuration) ){
			Move (currentMoveReactionDirection, dodgeSpeed);
		}
	}
	
	/*
	 * Interrupt fighter, push them back, and stun them for a second. This
	 * method only puts the fighter in the knockedback state and clears any
	 * other conflicting states it might have set.
	 */
	void ReceiveKnockback (Vector3 direction, float duration)
	{
		characterState = CharacterState.Knockedback;
		attackState = AttackState.None;
		lastKnockbackTime = Time.time;
		currentMoveReactionDirection = direction;
		currentMoveReactionDuration = duration;
	}
	
	/*
	 * When fighter is blocked, they get knocked back. For now this is identical
	 * to the knockback method.
	 */
	void UpdateKnockbackByBlockState ()
	{
		float timeInKnockbackState = Time.time - lastKnockbackTime;
		if (timeInKnockbackState >= currentMoveReactionDuration) {
			characterState = CharacterState.Idle;
		} else if (timeInKnockbackState < (KNOCKBACK_MOVE_PORTION * currentMoveReactionDuration) ){
			Move (currentMoveReactionDirection, dodgeSpeed);
		}
	}
	
	/*
	 * Set the fighter state to begin a knockback after getting blocked.
	 */
	void ReceiveKnockbackByBlock (Vector3 direction, float duration)
	{
		characterState = CharacterState.KnockedbackByBlock;
		attackState = AttackState.None;
		lastKnockbackTime = Time.time;
		currentMoveReactionDirection = direction;
		currentMoveReactionDuration = duration;
	}
	
	/*
	 * Pull the player out of flinching block state once the duration is up.
	 */
	void UpdateBlockingFlinchState ()
	{
		if (Time.time - lastFlinchTime >= currentFlinchDuration) {
			characterState = CharacterState.Idle;
		}
	}
	
	/*
	 * Successful blocks cause a flinch of a provided duration.
	 */
	void ReceiveBlockingFlinch (float duration)
	{
		characterState = CharacterState.BlockingFlinch;
		animation.Play (blockWindDown.name, PlayMode.StopAll);
		lastFlinchTime = Time.time;
		currentFlinchDuration = duration;
	}
	
	/*
	 * Set the fighter in Blocking state. Play animations and sounds.
	 */
	public void Block ()
	{
		if (IsIdle () || IsMoving () || IsAttacking ()) {
			SoundManager.Instance.PlayClipAtPoint (SoundManager.Instance.shield0, myTransform.position);
			animation.Play (blockWindUp.name, PlayMode.StopAll);
			animation.PlayQueued (blockIdle.name, QueueMode.CompleteOthers);
			characterState = CharacterState.Blocking;
		}
	}
	
	/*
	 * Set the fighter back to non-blocking state.
	 */
	public void UnBlock ()
	{
		characterState = CharacterState.Idle;
		animation.Play (blockWindDown.name, PlayMode.StopAll);
		SoundManager.Instance.PlayClipAtPoint (SoundManager.Instance.shieldDown0, myTransform.position);
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
			case CharacterState.BlockingFlinch:
				colorToShow = Color.grey;
				break;
			case CharacterState.Knockedback:
				colorToShow = Color.blue;
				break;
			case CharacterState.KnockedbackByBlock:
				colorToShow = Color.grey;
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
	 * Resolve a hit and perform the appropriate reaction. This may mean
	 * taking damage or it may mean resolving a block. 
	 */
	public void TakeHit (Attack attack, Transform attacker)
	{
		// Handle blocked hits first
		if (IsBlocking ()) {
			SoundManager.Instance.PlayClipAtPoint (SoundManager.Instance.blocked0, myTransform.position);
			// Cause blocker to get knocked back
			attacker.GetComponent<Fighter> ().ReceiveKnockbackByBlock ((attacker.position - myTransform.position).normalized, 0.3f);
			ReceiveBlockingFlinch (0.5f);
		} else {
			lastHitTime = Time.time;
			health.AdjustHealth (attack.damage);
		}
		// Handle reaction type of successful hits
		if (attack.reactionType == Attack.ReactionType.Knockback) {
			// Knock back in the opposite direction of the attacker.
			ReceiveKnockback ((myTransform.position - attacker.position).normalized, attack.knockbackDuration);
		} else if (attack.reactionType == Attack.ReactionType.Flinch) {
			ReceiveFlinch (attack.flinchDuration);
		}
	}

	public void NotifyAttackHit ()
	{
		//GameManager.Instance.FreezeGame (0.067f);
	}
}