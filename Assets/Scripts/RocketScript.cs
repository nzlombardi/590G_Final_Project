using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketScript : MonoBehaviour {

	void Start () {
		Destroy(gameObject, 3);		
	}

	void OnCollisionEnter (Collision target){
		if(target.gameObject.name != "FPSController"){
			Destroy(gameObject);
		}
	}
}
