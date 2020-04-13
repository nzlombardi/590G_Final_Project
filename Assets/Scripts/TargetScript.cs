using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetScript : MonoBehaviour {

	private GameObject manager;
	
	void Start(){
		manager = GameObject.Find("GameManager");
	}

	void OnCollisionEnter(Collision target){
		if(target.gameObject.name == "rocket"){
			manager.GetComponent<ManagerScript>().score++;
			Debug.Log("Target hit.");
			Destroy(gameObject);
		}
	}
}
