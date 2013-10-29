using UnityEngine;
using System;
using System.Collections;

public class Fighter : MonoBehaviour
{
	IController controller;
	public float movespeed; //TODO Rename me runspeed;
	public float sprintspeed;
	public Transform target;
	Stamina stamina;
	Health health;
	bool isHuman = false;

	public bool IsBlocking { get; private set; }

	// Animations
	public AnimationClip idle;
	public TrailRenderer swingTrail;
	public AnimationClip blockIdle;
	public AnimationClip blockBreak;

	public AudioClip blockSound;
	public AudioClip shieldUpSound;
	public AudioClip shieldBreakSound;
	public AudioClip takeHitSound;
	public AudioSource attackAndBlockChannel;

	// Attacks
	Attack[] attacks;
	Attack[] blockingAttacks;
	Attack[] currentAttackStance;
	Attack currentAttack;

	// The Team the fighter is on
	public enum Team
	{
		Neutral = 0,
		GoodGuys = 1,
		BadGuys = 2
	}
	public Team team;

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
		BrokenBlockFlinch,
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

	// Store variables for attack tracking
	float swingTime;
	float forcedAttackMoveSpeed;

	// Character control members
	Vector3 moveDirection;
	float gravity = -20.0f;
	float verticalSpeed = 0.0f;
	float damping = 25.0f;
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
	}

	void Start ()
	{
		// Initialize attacks
		AttackManager attackManager = (AttackManager)GameObject.Find (ObjectNames.MAANAGERS).GetComponent <AttackManager> ();
		attacks = new Attack[Enum.GetNames (typeof(AttackType)).Length];
		blockingAttacks = new Attack[Enum.GetNames (typeof(AttackType)).Length];
		if (isHuman) {
			attacks [(int)AttackType.Weak] = attackManager.GetAttack (Attacks.PLAYER_WEAK);
			attacks [(int)AttackType.Strong] = attackManager.GetAttack (Attacks.PLAYER_STRONG);
			blockingAttacks [(int)AttackType.Weak] = attackManager.GetAttack (Attacks.PLAYER_BLOCKING_WEAK);
			blockingAttacks [(int)AttackType.Strong] = attackManager.GetAttack (Attacks.PLAYER_BLOCKING_STRONG);
		} else {
			attacks [(int)AttackType.Weak] = attackManager.GetAttack (Attacks.ENEMY_WEAK);
			attacks [(int)AttackType.Strong] = attackManager.GetAttack (Attacks.ENEMY_STRONG);
		}
		currentAttackStance = attacks;
	}

	void Update ()
	{
		ApplyGravity ();
		PerformActionInProgress ();
		UpdateLockOn ();

		controller.Think ();
		TryDebugs ();

		// Animation sector
		if (IsIdle () || IsMoving ()) {
			if (IsBlocking) {
				animation.CrossFade (blockIdle.name, 0.1f);
			} else {
				animation.Play (idle.name, PlayMode.StopAll);
			}
		} else if (IsDodging ()) {
			animation.Play (idle.name, PlayMode.StopAll);
		} else if (IsInMoveReaction ()) {
			// Interrupt or stop attack animation
			animation.Play (idle.name, PlayMode.StopAll);
		} else if (IsInFlinchReaction ()) {
			// Interrupt or stop attack animation
			if (characterState == CharacterState.BrokenBlockFlinch) {
				animation.CrossFade (blockBreak.name, 0.0f);
			} else {
				animation.Play (idle.name, PlayMode.StopAll);
			}
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
				// No animation plays during winddown, so that he poses on the last frame of the attack.
			}
		}

		RenderColor ();
	}

	/*
	 * Plays the specified sound clip as a one shot sound
	 */
	void PlayAttackSound(AudioClip clip)
	{
		attackAndBlockChannel.PlayOneShot(clip);
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
	 * Return true if the fighter is in the middle of a flinch
	 * caused by performing a successful block.
	 */
	bool IsBlockFlinching ()
	{
		return characterState == CharacterState.BlockingFlinch;
	}
	
	/*
	 * Return true if the fighter is in any flinching state.
	 */
	bool IsInFlinchReaction ()
	{
		return characterState == CharacterState.Flinching || characterState == CharacterState.BlockingFlinch ||
			characterState == CharacterState.BrokenBlockFlinch;
	}
	
	/*
	 * Return true if the character is in the middle of a Move Reaction (ex. knockback).
	 */
	bool IsInMoveReaction ()
	{
		return characterState == CharacterState.Knockedback || characterState == CharacterState.KnockedbackByBlock;
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
		} else if (IsInFlinchReaction ()) {
			UpdateFlinchReactionState ();
		} else if (IsInMoveReaction ()) {
			UpdateMoveReactionState ();
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
	 * Assigns movement to the character, which is applied in the attack state update
	 */
	public void SetAttackMovement(float speed)
	{
		forcedAttackMoveSpeed = speed;
	}

	/*
	 * Walk the fighter in a given direction.
	 */
	public void Run (Vector3 direction)
	{
		if (IsIdle () || IsMoving ()) {
			characterState = CharacterState.Moving;
			float movescale = 1.0f;
			if (IsBlocking) {
				movescale = 0.5f;
			}
			Move (direction, movespeed * movescale);
		}
	}
	
	/*
	 * Sprint the fighter in a given direction. If fighter is out of stamina, just run.
	 */
	public void Sprint (Vector3 direction)
	{
		if (IsIdle () || IsMoving ()) {
			LoseTarget ();
			if (stamina.HasAnyStamina ()) {
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
		if(forcedAttackMoveSpeed > 0)
		{
			Move (transform.TransformDirection(Vector3.forward), forcedAttackMoveSpeed);
		}

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
			if (isActive) {
				attackCaster.Begin (currentAttack);
			} else {
				attackCaster.End ();
			}
		}
	}

	/*
	 * Cancels and cleans up after an active attack.
	 */
	void CancelAttack ()
	{
		// Stop the existing attack sound
		attackAndBlockChannel.Stop();
		// Clear any unfinished forced attack move speed
		forcedAttackMoveSpeed = 0;
		attackState = AttackState.None;
		swingTrail.renderer.enabled = false;
		SetAttackActive (false);
	}

	/*
	 * Sets the fighter as a human or AI
	 */
	public void SetHuman (bool human)
	{
		isHuman = human;
	}

	/*
	 * Try to make the character swing its weapon. If it's in the process
	 * of swinging or swing is on cooldown, it won't do anything.
	 */
	public void SwingWeapon (AttackType attackType)
	{
		SwingWeapon (currentAttackStance [(int)attackType]);
	}

	public void SwingWeapon (Attack attack)
	{
		if (stamina.HasAnyStamina () && (IsIdle () || IsMoving ())) {
			stamina.UseStamina (attack.stamina);
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
		if (stamina.HasAnyStamina () && (IsMoving () || IsIdle ())) {
			if (IsAttacking ()) {
				CancelAttack ();
			}
			if (IsBlocking) {
				UnBlock ();
			}
			currentDodgeDirection = direction;
			characterState = CharacterState.Dodging;
			lastDodgeTime = Time.time;

		}
	}
	
	/*
	 * Use a timer to stun a character for a configurable amount of time.
	 */
	void UpdateFlinchReactionState ()
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
		if (IsAttacking ()) {
			CancelAttack ();
		}
		characterState = CharacterState.Flinching;

		lastFlinchTime = Time.time;
		currentFlinchDuration = duration;
	}
	
	/*
	 * Knockback consists of a pushback stage and stun stage. This method uses
	 * timers for both to determine which it is in. When both timers are up,
	 * return the fighter to Idle, allowing movement again.
	 */
	void UpdateMoveReactionState ()
	{
		float timeInKnockbackState = Time.time - lastKnockbackTime;
		if (timeInKnockbackState >= currentMoveReactionDuration) {
			characterState = CharacterState.Idle;
		} else if (timeInKnockbackState < (KNOCKBACK_MOVE_PORTION * currentMoveReactionDuration)) {
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
		if (IsAttacking ()) {
			CancelAttack ();
		}
		characterState = CharacterState.Knockedback;
		lastKnockbackTime = Time.time;
		currentMoveReactionDirection = direction;
		currentMoveReactionDuration = duration;
	}

	/*
	 * Set the fighter state to begin a knockback after getting blocked.
	 */
	void ReceiveKnockbackByBlock (Vector3 direction, float duration)
	{
		if (IsAttacking ()) {
			CancelAttack ();
		}
		characterState = CharacterState.KnockedbackByBlock;
		lastKnockbackTime = Time.time;
		currentMoveReactionDirection = direction;
		currentMoveReactionDuration = duration;
	}
	
	/*
	 * Blocks that are broken cause a big flinch.
	 */
	void ReceiveBrokenBlockFlinch (float duration)
	{
		UnBlock ();
		characterState = CharacterState.BrokenBlockFlinch;
		lastFlinchTime = Time.time;
		currentFlinchDuration = duration;
	}
	
	/*
	 * Set the fighter in Blocking state. Play animations and sounds.
	 */
	public void Block ()
	{
		if (IsBlocking) {
			return;
		}

		if (IsIdle () || IsMoving ()) {
			PlayAttackSound (shieldUpSound);
			IsBlocking = true;
			currentAttackStance = blockingAttacks;
			if (IsAttacking ()) {
				characterState = CharacterState.Idle;
				CancelAttack ();
			}
		}
	}
	
	/*
	 * Set the fighter back to non-blocking state.
	 */
	public void UnBlock ()
	{
		currentAttackStance = attacks;
		IsBlocking = false;
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
	public Attack[] GetAttacks ()
	{
		return attacks;
	}
	
	/*
	 * Reduce stamina by a formula depending on the damage of the incoming attack and
	 * return whether or not the block succeeded. Failed blocks should result in the
	 * player entering a broken block state (serious stun).
	 */
	bool CheckBlockStamina (Attack attack)
	{
		if (stamina.HasStamina (attack.stamina)) {
			stamina.UseStamina (attack.stamina);
			return true;
		} else {
			stamina.UseStamina (attack.stamina);
			return false;
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
			case CharacterState.BrokenBlockFlinch:
				colorToShow = Color.red;
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
	public void TakeHit (RaycastHit hit, Attack attack, Transform attacker)
	{
		// Handle blocked hits first
		if (IsBlocking) {
			if (CheckBlockStamina (attack)) {
				PlayAttackSound (blockSound);
				// Cause attacker to get knocked back
				attacker.GetComponent<Fighter> ().ReceiveKnockbackByBlock ((attacker.position - myTransform.position).normalized, 0.3f);
			} else {
				PlayAttackSound (shieldBreakSound);
				ReceiveBrokenBlockFlinch (2.0f);
			}
		} else {
			// Play a new hit sound at the location. Must make minDistance the same as the
			// attack channel so that it plays at the same volume. This is kind of weird...
			AudioSource source = SoundManager.PlayClipAtPoint(takeHitSound, hit.point);
			source.minDistance = attackAndBlockChannel.minDistance;

			lastHitTime = Time.time;
			health.AdjustHealth (-attack.damage);
			// Handle reaction type of successful hits
			if (attack.reactionType == Attack.ReactionType.Knockback) {
				// Knock back in the opposite direction of the attacker.
				ReceiveKnockback ((myTransform.position - attacker.position).normalized, attack.knockbackDuration);
			} else if (attack.reactionType == Attack.ReactionType.Flinch) {
				ReceiveFlinch (attack.flinchDuration);
			}
		}
	}

	public void NotifyAttackHit ()
	{
		//GameManager.Instance.FreezeGame (0.067f);
	}
}