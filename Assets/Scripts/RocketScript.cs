using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityStandardAssets.Characters.FirstPerson;


public class RocketScript : MonoBehaviour {
	public int time_flight;
	public int explosion_radius;
	public int explosion_force;

	private GameObject player;
	private FirstPersonController fps;

	void Start () {
		player = GameObject.Find("FPSController");
		fps = player.GetComponent<FirstPersonController>();
		Destroy(gameObject, time_flight);		
	}

	void OnCollisionEnter (Collision target){
		if(target.gameObject.tag != "Player"){	
			float distance_to_player = Vector3.Distance(transform.position, player.transform.position);
			//Debug.Log("Rocket exploded. Distance to player: " + distance_to_player.ToString());
			if(distance_to_player <= explosion_radius){
				Vector3 force = (player.transform.position-transform.position) * (1-distance_to_player/explosion_radius) * explosion_force;
				fps.m_RocketJump = true;
				fps.SetForce(force);
				Debug.Log("Rocket jump. Applying force: " + force.ToString());
			}
			Destroy(gameObject);
		}
		
		if(target.gameObject.tag == "Player"){
			Debug.Log("Rocket hit player");
			Physics.IgnoreCollision(target.collider, GetComponent<Collider>());
		}
	}
}
