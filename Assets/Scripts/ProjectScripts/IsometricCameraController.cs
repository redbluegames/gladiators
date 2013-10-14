using UnityEngine;
using System.Collections;

public class IsometricCameraController : MonoBehaviour
{
	public GameObject target;
	public int viewPortIndex;

	// Position offsets from the camera
	public int xOffset = 0;
	public int yOffset = 11;
	public int zOffset = 0;

	public int xRotation = 90;
	public int yRotation = 0;
	public int zRotation = 0;
 
	void Start ()
	{
	}

	void LateUpdate ()
	{
		// Set the offset this camera uses when following its target
		Vector3 positionOffset = new Vector3(xOffset, yOffset, zOffset);
		transform.position = target.transform.position + positionOffset;

		// Set the rotation this camera uses when following its target
		// NOTE: This only needs to be handled on update so that the rotation reflects public
		// variables when changed during gameplay
		Vector3 rotationOffset = new Vector3(xRotation, yRotation, zRotation);
		transform.rotation = Quaternion.Euler(rotationOffset);
	}

	/*
	 * Split the cameras up among the players.
	 */
	public void SplitScreenView (int numViewports)
	{
		float border = 0.002f;
		float portion = (1.0f / numViewports) - (border);
		float spacing = viewPortIndex * border;
		camera.rect = new Rect ((viewPortIndex * portion) + spacing, 0, portion, 1);
	}
}
