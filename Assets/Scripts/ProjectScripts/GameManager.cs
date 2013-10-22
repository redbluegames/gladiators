using UnityEngine;
using System.Collections;

/*
 * Code pattern for this class borrowed from:
 * http://wiki.unity3d.com/index.php/Singleton
 *
 * This is our "god class" for handling managers and game-global variables.
 **/
public class GameManager : Singleton<GameManager>
{
	// Time and Pause handling members
	int pauseRequests;
	float lastPauseTime;
	public bool IsPaused { get; private set; }
	float timeToUnfreeze;
	bool isFrozen;

	// guarantee this will be always a singleton only - can't use the constructor!
	protected GameManager ()
	{
	}

	void Update ()
	{
		// Try to unfreeze a frozen game
		if (isFrozen) {
			if (Time.realtimeSinceStartup >= timeToUnfreeze) {
				Unfreeze();
			}
		}
	}


	/*
	 * Freeze the game for a specified amount of time. This behaves nicely with Pause.
	 */
	public void FreezeGame(float time)
	{
		// Do not freeze the game if we are paused
		if (IsPaused) {
			return;
		}
		// Don't let a second freeze call interrupt a longer, earlier freeze call
		float requestedTimeToUnfreeze = Time.realtimeSinceStartup + time;
		timeToUnfreeze = Mathf.Max(timeToUnfreeze, requestedTimeToUnfreeze);
		Time.timeScale = 0.0f;
		isFrozen = true;
	}

	/*
	 * Internally unfreezes the game
	 */
	private void Unfreeze ()
	{
		Time.timeScale = 1.0f;
		isFrozen = false;
	}

	/*
	 * Change the pause state of the game. This behaves nicely with Freezing.
	 */
	private void SetPaused (bool pause)
	{
		if (pause) {
			// Stamp off the time the game was first paused
			if (!IsPaused) {
				lastPauseTime = Time.realtimeSinceStartup;
			}
			Time.timeScale = 0.0f;
		} else {
			// If unpausing from frozen, we need to be sure to resume the freeze
			if (isFrozen) {
				float totalTimePaused = Time.realtimeSinceStartup - lastPauseTime;
				timeToUnfreeze += totalTimePaused;
			} else {
				Time.timeScale = 1.0f;
			}
		}
		IsPaused = pause;
	}

	/*
	 * Pauses the game, or increments the pause counter if it's already paused.
	 */
	public void RequestPause ()
	{
		pauseRequests++;
		SetPaused (true);
	}

	/*
	 * Attempts to unpause the game. Once all requests to pause have been unwound, the game
	 * unpauses.
	 */
	public void RequestUnpause ()
	{
		pauseRequests--;
		if (pauseRequests == 0) {
			SetPaused (false);
		}
	}
}
