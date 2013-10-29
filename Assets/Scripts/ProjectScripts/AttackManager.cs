using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AttackManager : MonoBehaviour
{
	public List<Attack> attackList = new List<Attack> ();

	public int Count ()
	{
		return attackList.Count;
	}
	
	public Attack GetAttack (int attackId)
	{
		return attackList[attackId];
	}
	
	public void AddAttack (Attack attack)
	{
		attackList.Add (attack);
	}
	
	public void ClearAttacks ()
	{
		attackList.Clear ();
	}
}
