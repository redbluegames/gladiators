using UnityEngine;
using System.Collections;

public class Arena : Singleton<Arena>
{
	public int curWave = 0;
	public Transform spawnPoint;
	public float spawnRange = 10.0f;
	
	// This might belong in the wave class mentioned below
	public int waveTimeAllowed = 60;
	public float waveTimeLeft;
	
	// TODO Let's make a new class for waves that allows a wave to be composed of
	// any number of any type of enemy;
	public GameObject enemyPrefab;
	public int[] waveComp = {2,4,6,8,10,12,14,16,18,20};

	public bool IsRunning {get; private set;}
	
	public UICenterText arenaText;
	
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
		if (!PlayersAlive ()) {
			arenaText.DisplayMessage ("Game Over!", Mathf.Infinity);
			return;
		}
		if (GetActiveEnemyCount () == 0 && !IsRunning) {
			arenaText.DisplayMessage ("You Win!", Mathf.Infinity);
		}
		if(!IsRunning) {
			return;
		}
		TickTimer ();
		if (IsTimeUp ()) {
			StartNextWave ();
		} else if (AllEnemiesKilled () && waveTimeLeft > 3.0f) {
			arenaText.DisplayMessage ("All enemies killed. Skipping to next wave.", 3.0f);
			waveTimeLeft = 3.0f;
		}
	}
	
	/*
	 * Handle all logic around creating a new wave of enemies.
	 */
	public void StartNextWave ()
	{
		arenaText.DisplayMessage ("Incoming Wave!", 2.5f);
		if(waveComp.Length == 0)
		{
			IsRunning = false;
			return;
		}
		SpawnWave ();
		curWave++;
		if (curWave > waveComp.Length - 1) {
			IsRunning = false;
			return;
		}
		waveTimeLeft = waveTimeAllowed;
		IsRunning = true;
	}

	/*
	 * Spawn the current wave of enemies into the arena at random locations around our spawn.
	 */
	void SpawnWave ()
	{
		for (int i = 0; i < waveComp[curWave]; i++) {
			float randX = Random.Range (-spawnRange, spawnRange);
			float randZ = Random.Range (-spawnRange, spawnRange);
			Vector3 randomDistance = new Vector3 (randX, 0, randZ);
			Instantiate (enemyPrefab, (spawnPoint.position + randomDistance), spawnPoint.rotation);
		}
	}
	
	/*
	 * Update our timers and check if time is up.
	 */
	void TickTimer ()
	{
		waveTimeLeft -= Time.deltaTime;
	}

	bool IsTimeUp ()
	{
		return waveTimeLeft <= 0;
	}
	
	bool AllEnemiesKilled ()
	{
		return GetActiveEnemyCount () == 0;
	}
	
	/*
	 * Return true if any player is alive.
	 */
	bool PlayersAlive ()
	{
		GameObject player = GameObject.FindGameObjectWithTag (Tags.PLAYER);
		return player != null;
	}
	
	/*
	 * Find the number of active enemy objects and return the count.
	 */
	public int GetActiveEnemyCount ()
	{
		return GameObject.FindGameObjectsWithTag (Tags.ENEMY).Length;
	}
}