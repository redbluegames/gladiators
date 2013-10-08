using UnityEngine;
using System.Collections;

public class RBInput {

    const string PLAYER_PREFIX = "_P";
    const string DEVICE_PREFIX = "_";

    public static bool GetButtonDownForPlayer(string buttonName, int playerIndex, InputDevice device)
    {
        return Input.GetButtonDown (ConcatPlayerIndex (buttonName, playerIndex, device));
    }

    public static bool GetButtonForPlayer(string buttonName, int playerIndex, InputDevice device)
    {
        return Input.GetButton (ConcatPlayerIndex (buttonName, playerIndex, device));
    }

    public static float GetAxisRawForPlayer (string axisName, int playerIndex, InputDevice device)
    {
        return Input.GetAxisRaw (ConcatPlayerIndex(axisName, playerIndex, device));
    }

    public static float GetAxisForPlayer (string axisName, int playerIndex, InputDevice device)
    {
        return Input.GetAxis (ConcatPlayerIndex (axisName, playerIndex, device));
    }

    static string ConcatPlayerIndex (string buttonName, int playerIndex, InputDevice device)
    {
        return buttonName + DEVICE_PREFIX + device.DeviceName + PLAYER_PREFIX + playerIndex.ToString ();
    }
}
