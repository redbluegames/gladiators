using UnityEngine;
using System.Collections;

public class Arena : Singleton<Arena>
{
	public int totalWaves = 10;
	public int curWave = 0;
	public int[] waveComp = {2,4,6,8,10,12,14,16,18,20};
	
	public Transform spawnPoint;
	
	// guarantee this will be always a singleton only - can't use the constructor!
	protected Arena ()
	{
	}
	
	public void StartNextWave ()
	{
		
	}
}
