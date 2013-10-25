using UnityEngine;
using System.Collections;

public class SoundManager : Singleton<SoundManager> {

	public AudioClip shield0;
	public AudioClip blocked0;
	public AudioClip shieldDown0;
	public AudioClip shieldBreak;
	public AudioClip swingLight0;
	public AudioClip swingHit0;
	
	public static void PlayClipAtPoint (AudioClip sound, Vector3 point)
	{
		AudioSource.PlayClipAtPoint (sound, point);
	}
}
