using UnityEngine;
using System.Collections;

public class SoundManager : Singleton<SoundManager> {

	public AudioClip shield0;
	public AudioClip blocked0;
	public AudioClip shieldDown0;
	public AudioClip shieldBreak;
	public AudioClip swingLight0;
	public AudioClip swingHit0;

	/*
	 * Plays a clip at a specified position and returns a reference to the AudioSource
	 */
	public static AudioSource PlayClipAtPoint (AudioClip sound, Vector3 point)
	{
		GameObject tempGameObject = new GameObject("TempAudio");
		// Assign attributes to object and audio source
		tempGameObject.transform.position = point;
		AudioSource audioSource = tempGameObject.AddComponent<AudioSource>();
		audioSource.clip = sound;
		audioSource.Play();
		// Set object to destroy after clip plays
		Destroy(tempGameObject, sound.length);
		return audioSource;
	}
}
