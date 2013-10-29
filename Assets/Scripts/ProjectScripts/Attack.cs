using UnityEngine;
using System.Collections;

[System.Serializable]
public class Attack : ScriptableObject
{
	int id;
	public int damage;
	public float range;
	public float stamina;
	public ReactionType reactionType;
	public float flinchDuration;
	public float knockbackDuration;
	public float windupTime;
	public float winddownTime;
	public AnimationClip swing;
	public AnimationClip windup;
	public AnimationClip winddown;
	public AudioClip soundEffect;
	
	public enum ReactionType {
		None,
		Flinch,
		Knockback
	}
}
