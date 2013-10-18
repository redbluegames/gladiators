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
	// guarantee this will be always a singleton only - can't use the constructor!
	protected GameManager ()
	{
	}
	
	public Arena arena;
 
	void Awake ()
	{
		arena = Instance.GetComponent<Arena> ();
		if (arena == null) {
			Debug.Log ("Adding Arena Component to Manager since it wasn't attached via the Editor.");
			arena = gameObject.AddComponent<Arena> ();
		}
	}
}
