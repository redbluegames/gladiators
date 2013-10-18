using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
	public int PlayerIndex { get; private set; }
	public InputDevice playerDevice { get; private set; }
	
	Fighter fighter;
	bool isPlayerBound;

	void Awake ()
	{
		fighter = gameObject.GetComponent<Fighter> ();
		isPlayerBound = false;

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
		
		TryMove ();
		TryAttack();
		TryDebugs ();
	}

	private void LateUpdate ()
	{
	}

	/*
	 * Apply movement in the Player's desired directions according to the various speed
	 * and movement variables.
	 */
	void TryMove ()
	{
		// Get input values
		float horizontal = 0.0f, vertical = 0.0f;
		horizontal = RBInput.GetAxisRawForPlayer (InputStrings.HORIZONTAL, PlayerIndex, playerDevice);
		vertical = RBInput.GetAxisRawForPlayer (InputStrings.VERTICAL, PlayerIndex, playerDevice);
		
		Vector3 direction = new Vector3 (horizontal, 0.0f, vertical);
		fighter.Walk (direction);
	}

	void TryAttack()
	{
		bool isAttack = RBInput.GetButtonDownForPlayer(InputStrings.ATTACK, PlayerIndex, playerDevice);
		if(isAttack)
		{
			fighter.SwingWeapon();
		}
	}

	/*
	 * Reads input and handles action for all debug functions
	 */
	void TryDebugs ()
	{
	}

	public void BindPlayer (int index, InputDevice device)
	{
		isPlayerBound = true;

		PlayerIndex = index;
		playerDevice = device;
	}
}