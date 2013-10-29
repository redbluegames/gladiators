using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class AttackDatabaseManager : EditorWindow
{
	const string ATTACK_CSV = "Attacks";
	readonly string[] columnNames = {"Name", "Damage", "Range", "Reaction Type", "Flinch Duration", "Knockback Duration",
		"Windup Time","Winddown Time","Swing Animation","Windup Animation","Winddown Animation"};

	[MenuItem("RedBlue Tools/Import Attacks CSV")]
	static void Init ()
	{
		AttackDatabaseManager window = (AttackDatabaseManager) EditorWindow.CreateInstance (typeof (AttackDatabaseManager));
		window.Show ();
	}
	
	void OnGUI ()
	{
		if (GUILayout.Button ("Import Attacks")) {
			ImportAttacks ();
		}
	}
	
	/*
	 * Read in a CSV Attack file generated from our Google Spreadsheet of attacks. Each
	 * line will be an Attack, stored as a comma delimited value for each field in our Attack class.
	 * Once complete, write the name of each attack in a lookup file of strings.
	 */
	void ImportAttacks ()
	{
		TextAsset attackCsv = (TextAsset) Resources.Load (ATTACK_CSV, typeof (TextAsset));
		if (attackCsv == null) {
			Debug.LogError (string.Format ("Attack file {0} failed to load correctly.", ATTACK_CSV));
			return;
		}
		AttackManager.Instance.ClearAttacks ();
		string[] lineArray = attackCsv.text.Split ("\n" [0]);
		VerifyHeaderLine (lineArray [0]);
		for (int i = 1; i < lineArray.Length; i++) {
			Attack newAttack = SerializeAttackLine (lineArray [i]);
			AttackManager.Instance.AddAttack (newAttack);
		}
		WriteAttacksToStringFile ();
	}
	
	/*
	 * Ensure that the header line of columns matches the expected headers. If they
	 * don't match, we can't guarantee that the Attack fields will be mapped correctly.
	 */
	void VerifyHeaderLine (string line)
	{
		string[] headerArray = line.Split ("," [0]);
		if (headerArray.Length != columnNames.Length) {
			Debug.LogError (string.Format (
				"Number of CSV fields doesn't match expected. #Columns in CSV [{0}], #Expected [{1}].",
				headerArray.Length, columnNames.Length));
		}
		for (int i = 0; i < headerArray.Length; i++) {
			if (!string.Equals (headerArray [i], columnNames [i])) {
				Debug.LogError (string.Format (
					"Headers in Attack CSV did not match expected name. Expected [{0}], Actual [{1}].\n" +
					"Make sure the column orders match the expected order, otherwise there will be mismatched data.\n\n",
					headerArray [i], columnNames [i]));
			}
		}
	}
	
	/*
	 * Map every value in a provided line of comma-separated values to an Attack field.
	 * Instantiate the new Attack and attach it to our Attack Manager.
	 */
	Attack SerializeAttackLine (string line)
	{
		string[] fieldArray = line.Split ("," [0]);
		Attack newAttack = (Attack)ScriptableObject.CreateInstance<Attack> ();
		newAttack.name = fieldArray [0];
		newAttack.damage = int.Parse (fieldArray [1]);
		newAttack.range = float.Parse (fieldArray [2]);
		newAttack.reactionType = SerializeReactionType (fieldArray [3]);
		newAttack.flinchDuration = float.Parse (fieldArray [4]);
		newAttack.knockbackDuration = float.Parse (fieldArray [5]);
		newAttack.windupTime = float.Parse (fieldArray [6]);
		newAttack.winddownTime = float.Parse (fieldArray [7]);
		newAttack.swing = LoadClipFromString (fieldArray [8]);
		newAttack.windup = LoadClipFromString (fieldArray [9]);
		newAttack.winddown = LoadClipFromString (fieldArray [10]);
		return newAttack;
	}
	
	/*
	 * Write our resultant attacks to a lookup file so each can be referenced by name.
	 */
	void WriteAttacksToStringFile ()
	{
		string path = "./Assets/Scripts/ProjectScripts/Strings/Attacks.cs";
		string createText = "using UnityEngine;\nusing System.Collections;" +
				"\n\npublic class Attacks\n{\n";
		System.IO.File.WriteAllText (path, createText);
		const string keywords = "\tpublic static int";
		int attackId = 0;
		foreach (Attack attack in AttackManager.Instance.attackList) {
			string attackLine = string.Format ("{0} {1} = {2};\n", keywords, 
				attack.name.ToUpper ().Replace (' ', '_'), attackId);
			System.IO.File.AppendAllText (path, attackLine);
			attackId++;
		}
		System.IO.File.AppendAllText (path, "}");
		Debug.Log ("File Updated: " + path);
	}
	
	/*
	 * Maps each ReactionType string to its corresponding enum value.
	 */
	Attack.ReactionType SerializeReactionType (string typeString)
	{
		if (string.Equals (typeString, "Flinch")) {
			return Attack.ReactionType.Flinch;
		} else if (string.Equals (typeString, "Knockback")) {
			return Attack.ReactionType.Knockback;
		} else {
			return Attack.ReactionType.None;
		}
	}
	
	/*
	 * Helper method that loads an animation clip using a provided clip name.
	 * This may need refactoring later as it may be better to just store a string
	 * until the animation is needed.
	 */
	AnimationClip LoadClipFromString (string clipname)
	{
		// Note this is not the most efficient time to load new animations as 
		// EVERY single attack animation will be loaded when we run the game.
		return (AnimationClip) Resources.Load (clipname, typeof(AnimationClip));
	}
}
