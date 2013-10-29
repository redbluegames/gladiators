using UnityEngine;
using System.Collections;

public class Bandages : MonoBehaviour
{
	public int maxBandages = 3;
	public int bandageCount = 3;
	public float healAmount = 25.0f;
	float timeToBandage = 3.0f;
	float timeSpentBandaging = 0.0f;
	const float NOT_BANDAGING = 0.0f;
	
	public void AddBandage ()
	{
		if (bandageCount < maxBandages) {
			bandageCount++;
		} else {
			Debug.LogWarning ("Attempted to Add bandage when already maxed out. Let's avoid this.");
		}
	}
	
	public void RemoveBandage ()
	{
		if (bandageCount > 0) {
			bandageCount--;
		} else {
			bandageCount = 0;
			Debug.LogWarning ("Attempted to Remove bandage when player had none. Let's avoid this.");
		}
	}
	
	public bool IsApplyingTime ()
	{
		return timeSpentBandaging != NOT_BANDAGING;
	}
	
	public bool HasBandages ()
	{
		return bandageCount > 0;
	}
	
	public bool ApplyBandages (float timeSpent)
	{
		AdjustBandageTime (timeSpent);
		if (timeSpentBandaging == timeToBandage) {
			RemoveBandage ();
			timeSpentBandaging = NOT_BANDAGING;
			return true;
		}
		return false;
	}
	
	public void StopBandaging ()
	{
		timeSpentBandaging = NOT_BANDAGING;
	}
	
	protected void AdjustBandageTime (float adjustment)
	{
		if (timeSpentBandaging + adjustment > timeToBandage) {
			timeSpentBandaging = timeToBandage;
		} else {
			timeSpentBandaging += adjustment;
		}
	}
	
	public float CalculateDisplayPercent ()
	{
		return timeSpentBandaging / timeToBandage;
	}
}
