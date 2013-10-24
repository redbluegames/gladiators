using UnityEngine;
using System.Collections;

public class SoundManager : Singleton<SoundManager> {

	public AudioClip shield0;
	public AudioClip blocked0;
	public AudioClip shieldDown0;
	
	public void PlayClipAtPoint (AudioClip sound, Vector3 point)
	{
		AudioSource.PlayClipAtPoint (sound, point);
	}
}
