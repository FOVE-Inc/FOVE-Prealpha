using UnityEngine;
using System.Collections;

public class FOVELookSample : MonoBehaviour {

	Collider my_collider;

	// Use this for initialization
	void Start () {
		my_collider = GetComponent<Collider> ();
	}
	
	// Update is called once per frame
	void Update () {
		if (FOVEInterface.IsLookingAtCollider(my_collider))
		{
			gameObject.GetComponent<Renderer> ().material.color = Color.red;
			//bool check = FOVEInterface.IsLookingAtCollider(my_collider);
		} else
		{
			gameObject.GetComponent<Renderer> ().material.color = Color.white;
		}
	}
}
