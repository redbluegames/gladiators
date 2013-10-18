using UnityEngine;
using System.Collections;

public class Arena : Singleton<Arena>
{
	public int curWave = 0;
	public Transform spawnPoint;
	public float spawnRange = 10.0f;
	
	// This might belong in the wave class mentioned below
	public int waveTimeAllowed = 60;
	public int waveTimeLeft;
	float waveStartTime;
	bool timesUp = false;
	
	// TODO Let's make a new class for waves that allows a wave to be composed of
	// any number of any type of enemy;
	public GameObject enemyPrefab;
	public int[] waveComp = {2,4,6,8,10,12,14,16,18,20};
	
	// guarantee this will be always a singleton only - can't use the constructor!
	protected Arena ()
	{
	}
	
	void Awake () 
	{
		if (spawnPoint == null) {
			Debug.LogError ("Spawn Point for Arena not set. Attach spawn point to Arena script.");
		}
		if (enemyPrefab == null) {
			Debug.LogError ("Enemy Prefab for Arena not set. Attach prefab to Arena script.");
		}
		StartNextWave ();
	}
	
	void Update ()
	{
		CheckTime ();
		if (timesUp) {
			StartNextWave ();
		}
	}
	
	/*
	 * Handle all logic around creating a new wave of enemies.
	 */
	public void StartNextWave ()
	{
		if (curWave == waveComp.Length -1) {
			Debug.LogWarning ("Wave composition for CurWave not defined. Skipping StartNextWave.");
			return;
		}
		SpawnWave ();
		curWave++;
		waveStartTime = Time.time;
		timesUp = false;
	}
	
	/*
	 * Spawn the current wave of enemies into the arena at random locations around our spawn.
	 */
	void SpawnWave ()
	{
		for (int i = 0; i < waveComp[curWave]; i++) {
			float randX = Random.Range (-spawnRange, spawnRange);
			float randY = Random.Range (-spawnRange, spawnRange);
			Vector3 randomDistance = new Vector3 (randX, 0, randY);
			Instantiate (enemyPrefab, (spawnPoint.position + randomDistance), spawnPoint.rotation);
		}
	}
	
	/*
	 * Update our timers and check if time is up.
	 */
	void CheckTime ()
	{
		int timeElapsed = Mathf.FloorToInt (Time.time - waveStartTime);
		waveTimeLeft = Mathf.CeilToInt (waveTimeAllowed - timeElapsed);
		// TODO Is rounding up good enough precision?
		if (waveTimeLeft == 0) {
			timesUp = true;
		}
	}
	
	/*
	 * Find the number of active enemy objects and return the count.
	 */
	public int GetActiveEnemyCount ()
	{
		return GameObject.FindGameObjectsWithTag (Tags.ENEMY).Length;
	}
}