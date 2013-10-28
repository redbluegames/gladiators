using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AttackDatabaseManager : EditorWindow
{
	AttackManager attackManager;
	
	string newAttackName = string.Empty;
	float newAttackRange = 0.0f;
	int newAttackDamage = 0;
	float newAttackWindupTime = 0.0f;
	float newAttackWinddownTime = 0.0f;
	AnimationClip newAttackSwing = null;
	AnimationClip newAttackWindup = null;
	AnimationClip newAttackWinddown = null;
	float newAttackFlinchDuration = 0.0f;
	float newAttackKnockbackDuration = 0.0f;
	Attack.ReactionType newAttackReactionType = Attack.ReactionType.None;

	[MenuItem("RedBlue Tools/Attack Database Manager")]
	static void Init ()
	{
		AttackDatabaseManager window = (AttackDatabaseManager) EditorWindow.CreateInstance(typeof(AttackDatabaseManager));
		window.Show ();
	}
	
	void Awake ()
	{
	}
	
	void OnGUI ()
	{
		if (attackManager == null) {
			attackManager = GameObject.Find (ObjectNames.MAANAGERS).GetComponent<AttackManager> ();
		}
		newAttackName = EditorGUILayout.TextField ("Name: ", newAttackName);
		newAttackDamage = EditorGUILayout.IntField ("Damage: ", newAttackDamage);
		newAttackRange = EditorGUILayout.FloatField ("Range: ", newAttackRange);
		newAttackReactionType = (Attack.ReactionType) EditorGUILayout.EnumPopup ("Reaction Type: ", newAttackReactionType);
		if (newAttackReactionType == Attack.ReactionType.Flinch) {
			newAttackFlinchDuration = EditorGUILayout.FloatField ("Flinch Duration: ", newAttackFlinchDuration);
			newAttackKnockbackDuration = 0.0f;
		} else if (newAttackReactionType == Attack.ReactionType.Knockback) {
			newAttackKnockbackDuration = EditorGUILayout.FloatField ("Knockback Duration: ", newAttackKnockbackDuration);
			newAttackFlinchDuration = 0.0f;
		} else {
			newAttackKnockbackDuration = 0.0f;
			newAttackFlinchDuration = 0.0f;
		}
		newAttackWindupTime = EditorGUILayout.FloatField ("Windup Time: ", newAttackWindupTime);
		newAttackWinddownTime = EditorGUILayout.FloatField ("Winddown Time: ", newAttackWinddownTime);
		
		newAttackSwing = (AnimationClip) EditorGUILayout.ObjectField ("Swing Animation: ", newAttackSwing,
			typeof (AnimationClip), false);
		newAttackWindup = (AnimationClip) EditorGUILayout.ObjectField ("Windup Animation: ", newAttackWindup,
			typeof (AnimationClip), false);
		newAttackWinddown = (AnimationClip) EditorGUILayout.ObjectField ("Winddown Animation: ", newAttackWinddown,
			typeof (AnimationClip), false);
		
		if (GUILayout.Button ("Add New Attack"))
		{
			CleanupEmptyAttacks ();
		
			// Validate our GUI form
			if (newAttackName == string.Empty) {
				Debug.LogWarning ("Attack Name must be provided.");
				return;
			}
			
			Attack newAttack = (Attack) ScriptableObject.CreateInstance<Attack> ();
			newAttack.name = newAttackName;
			newAttack.damage = newAttackDamage;
			newAttack.range = newAttackRange;
			newAttack.swing = newAttackSwing;
			newAttack.windup = newAttackWindup;
			newAttack.winddown = newAttackWinddown;
			newAttack.flinchDuration = newAttackFlinchDuration;
			newAttack.knockbackDuration = newAttackKnockbackDuration;
			newAttack.reactionType = newAttackReactionType;
			newAttack.winddownTime = newAttackWinddownTime;
			newAttack.windupTime = newAttackWindupTime;
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
