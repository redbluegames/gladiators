using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AttackDatabase : MonoBehaviour
{
	public List<Attack> attackList;
	
	void Start ()
	{
		attackList = new List<Attack> ();
	}
	
	public Attack FindAttack (string name)
	{
		Attack result = attackList.Find (
		delegate(Attack attack)
		{
			return attack.name == name;
		});
		if (result == null)
		{
			Debug.Log ("not found");
		} else {
			Debug.Log (result.name);
		}
		return result;
	}
}
