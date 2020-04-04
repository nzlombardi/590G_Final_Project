using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetScript : MonoBehaviour {

	public GameObject manager;
	
	void OnCollisionEnter(Collision target){
		if(target.gameObject.name == "Rocket(Clone)"){
			Destroy(gameObject);
		}
	}
}
