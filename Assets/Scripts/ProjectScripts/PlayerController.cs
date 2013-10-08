using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
	public float movespeed;
	public float sprintspeed;
	private Vector3 moveDirection;
	private float gravity = -20.0f;
	private float verticalSpeed = 0.0f;
	private CollisionFlags collisionFlags;
	public int PlayerIndex { get; private set; }
	public InputDevice playerDevice { get; private set; }
	bool isPlayerBound;

	void Awake ()
	{
		isPlayerBound = false;
		moveDirection = transform.TransformDirection (Vector3.forward);

		BindPlayer(0, InputDevices.GetAllInputDevices()[(int)InputDevices.ControllerTypes.Keyboard]);
	}

	void Start ()
	{
	}

	void Update ()
	{
		if(!isPlayerBound)
		{
			return;
		}

		ApplyGravity ();

		Move ();
		TryDebugs ();
	}

	private void LateUpdate ()
	{
	}

	/*
	 * Sets vertical speed to the expected value based on whether or not the Player is grounded.
	 */
	private void ApplyGravity ()
	{
		if (IsGrounded ()) {
			verticalSpeed = 0.0f;
		} else {
			verticalSpeed += gravity * Time.deltaTime;
		}
	}

	/*
	 * Checks to see if the Player is grounded by checking collision flags.
	 */
	private bool IsGrounded ()
	{
		return (collisionFlags & CollisionFlags.CollidedBelow) != 0;
	}

	/*
	 * Apply movement in the Player's desired directions according to the various speed
	 * and movement variables.
	 */
	void Move ()
	{
		// Get input values
		float horizontal = 0.0f, vertical = 0.0f;
		horizontal = RBInput.GetAxisRawForPlayer (InputStrings.HORIZONTAL, PlayerIndex, playerDevice);
		vertical = RBInput.GetAxisRawForPlayer (InputStrings.VERTICAL, PlayerIndex, playerDevice);

		// Determine move direction from target values
		float targetSpeed = 0.0f;
		Vector3 targetDirection = new Vector3 (horizontal, 0.0f, vertical);
		if (targetDirection != Vector3.zero) {
			moveDirection = Vector3.RotateTowards (moveDirection, targetDirection, Mathf.Infinity, 1000);
			moveDirection = moveDirection.normalized;

			if(RBInput.GetButtonForPlayer(InputStrings.SPRINT, PlayerIndex, playerDevice))
			{
				targetSpeed = sprintspeed;
			}
			else
			{
				targetSpeed = movespeed;
			}
		}

		// Get movement vector
		Vector3 movement = (moveDirection * targetSpeed) + new Vector3 (0.0f, verticalSpeed, 0.0f);
		movement *= Time.deltaTime;

		// Apply movement vector
		CharacterController biped = GetComponent<CharacterController> ();
		collisionFlags = biped.Move (movement);

		// Rotate to face the direction of movement immediately
		if (moveDirection != Vector3.zero) {
			transform.rotation = Quaternion.LookRotation (moveDirection);
		}
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

	public void BindPlayer (int index, InputDevice device)
	{
		isPlayerBound = true;

		PlayerIndex = index;
		playerDevice = device;
	}
}