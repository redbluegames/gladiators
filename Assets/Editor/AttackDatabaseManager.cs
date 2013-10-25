using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AttackDatabaseManager : EditorWindow
{
	AttackManager attackManager;
	
	string newAttackName = string.Empty;
	float newAttackRange = 0.0f;
	float newAttackDamage = 0.0f;
	AnimationClip animation;
	/*
	public float range;
	public float windupTime;
	public float winddownTime;
	public AnimationClip swing;
	public AnimationClip windup;
	public AnimationClip winddown;
	public int damage;
	public float flinchDuration;
	public float knockbackDuration;
	public bool causeKnockback;
	public bool causeFlinch;
	public ReactionType reactionType;
	*/
	[MenuItem("RedBlue Tools/Attack Database Manager")]
	static void Init ()
	{
		AttackDatabaseManager window = (AttackDatabaseManager) EditorWindow.CreateInstance(typeof(AttackDatabaseManager));
		window.Show ();
	}
	
	void Awake ()
	{
		attackManager = GameObject.Find (ObjectNames.MAANAGERS).GetComponent<AttackManager> ();
	}
	
	void OnGUI ()
	{
		newAttackName = EditorGUILayout.TextField ("Name: ", newAttackName);
		newAttackDamage = EditorGUILayout.FloatField ("Damage: ", newAttackDamage);
		newAttackRange = EditorGUILayout.FloatField ("Range: ", newAttackRange);

		if (GUILayout.Button ("Add New Attack"))
		{
			CleanupEmptyAttacks ();
		
			// Validate
			if (newAttackName == string.Empty) {
				Debug.LogWarning ("Attack Name must be provided.");
				return;
			}

			Attack newAttack = (Attack) ScriptableObject.CreateInstance<Attack> ();
			newAttack.name = newAttackName;
			attackManager.AddAttack (newAttack);
			WriteAttacksToFile ();
		}
		if (GUILayout.Button ("Refresh"))
		{
			CleanupEmptyAttacks ();
			WriteAttacksToFile ();
		}
	}
	
	void WriteAttacksToFile ()
	{
		string path = "./Assets/Scripts/ProjectScripts/Strings/Attacks.cs";
		string createText = "using UnityEngine;\nusing System.Collections;" +
				"\n\npublic class Attacks\n{\n";
		System.IO.File.WriteAllText (path, createText);
		const string keywords = "\tpublic static int";
		int attackId = 0;
		foreach (Attack attack in attackManager.attackList) {
			string attackLine = string.Format ("{0} {1} = {2};\n", keywords, 
				attack.name.ToUpper ().Replace (' ', '_'), attackId);
			System.IO.File.AppendAllText (path, attackLine);
			attackId++;
		}
		System.IO.File.AppendAllText (path, "}");
		Debug.Log ("File Updated: " + path);
	}
	
	void CleanupEmptyAttacks ()
	{
		List<int> attacksToRemove = new List<int> ();
		int i = 0;
		foreach (Attack attack in attackManager.attackList) {
			if (attack == null) {
				attacksToRemove.Add (i);
			}
			i++;
		}
		// Careful here, we have to adjust our index as we remove. Since 
		// foreach iterates in a predictable (ascending) order, we know each removal
		// will move the remaining elements forward by one index.
		int numRemoved = 0;
		foreach (int index in attacksToRemove) {
			attackManager.attackList.RemoveAt (index - numRemoved);
			numRemoved++;
		}
	}
}
