using UnityEngine;
using System.Collections;

public class FOVE3DCursor : MonoBehaviour {
	
	// Use this for initialization
	void Start () {
	}
	
	// Latepdate ensures that the object doesn't lag behind the user's head motion
	void Update () {
		FOVEInterface.EyeRays rays = FOVEInterface.GetEyeRays ();

		// TODO: calculate the convergence point in FOVEInterface

		// Just hack in to use the left eye for now...
		transform.position = rays.left.GetPoint(10.0f);
	}
}
