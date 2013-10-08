using UnityEngine;
using System.Collections;

public class RBRandom
{

    /*
     * Rolls a random number and returns true at a rate in accordance with the passed in percent.
     */
    public static bool PercentageChance (int percent)
    {
        int rand = Random.Range (0, 100);
        return rand < percent;
    }

    /*
    * Rolls a random number and returns true at a rate in accordance with the passed in percent.
    */
    public static bool PercentageChance (float percent)
    {
        float rand = Random.Range (0, 100.0f);
        return rand < percent;
    }
}
