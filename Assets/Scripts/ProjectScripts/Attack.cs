using UnityEngine;
using System.Collections;

public class Attack : ScriptableObject
{
	public float range;
	public float windupTime;
	public float winddownTime;
	public AnimationClip swing;
	public AnimationClip windup;
	public AnimationClip winddown;
	public int damage;
}
