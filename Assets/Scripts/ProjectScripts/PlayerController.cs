using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
	public int PlayerIndex { get; private set; }
	public InputDevice playerDevice { get; private set; }
	
	public Fighter fighter;
	bool isPlayerBound;
	int curTarget;

	void Awake ()
	{
		fighter = gameObject.GetComponent<Fighter> ();
		isPlayerBound = false;
		curTarget = 0;
		HighlightArrow (false);

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
		TrySwitchTarget ();
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
		if (fighter.GetTarget () != null) {
			fighter.ZTargetMove (direction);
		} else {
			fighter.Walk (direction);
		}
	}
	
	/*
	 * If no target is selected, pick the first using the ENEMY tag. If one is selected
	 * already, choose the next until the end is reached at which point, select none. This
	 * will need to be refactored to be smarter later, i.e. choose the closest first, then
	 * switch further away. Also this should be a hold down button to keep lock, release to
	 * unlock behavior as well but right now it just tabs through.
	 */
	void TrySwitchTarget ()
	{
		if (RBInput.GetButtonDownForPlayer (InputStrings.TARGET, PlayerIndex, playerDevice)) {
			GameObject [] enemies = GameObject.FindGameObjectsWithTag (Tags.ENEMY);

			// Toggle to nothing when user has tabbed through the targets
			if (curTarget >= enemies.Length) {
				curTarget = 0;
				HighlightArrow (false);
				fighter.LoseTarget ();
				return;
			}
			
			// Select the next target
			HighlightArrow (true);
			fighter.LockOnTarget (enemies[curTarget].transform);
			curTarget++;
		}
	}

	void TryAttack()
	{
		bool isAttack = RBInput.GetButtonDownForPlayer(InputStrings.FIRE, PlayerIndex, playerDevice);
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
	
	/*
	 * Debug method that highlights the Arrow or not. Pass in True to highlight,
	 * False to not highlight it.
	 */
	void HighlightArrow (bool trueFalse)
	{
		Component[] renderers = GameObject.Find ("Arrow").GetComponentsInChildren<Renderer> ();
		foreach (Renderer renderer in renderers) {
			if (trueFalse) {
				renderer.material.color = Color.red;
			} else {
				renderer.material.color = Color.blue;
			}
			//renderer.enabled = trueFalse;
		}
	}
}