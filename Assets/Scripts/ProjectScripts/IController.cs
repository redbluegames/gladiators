using UnityEngine;
using System.Collections;

/*
 * Abstract class for Controllers like PlayerController and FighterAI.
 * This allows an NPC or player to have access to the AI or Controller Input.
 */
abstract public class IController : MonoBehaviour
{
	public abstract void Think ();
}
