using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LauncherScript : MonoBehaviour {

	public float fire_delay;
	public float projectile_speed;
	public Rigidbody rocket;
	public Rigidbody player;

	private float last_fired;
	private Vector3 last_pos;
	private Vector3 player_velocity;
	
	void Start(){
		last_pos = player.transform.position;
	}

	void Update () {
		//If fire key is pressed and enough time has passed since last shot, make another rocket and fire it
		if(Input.GetButtonDown("Fire1") && Time.time - last_fired > fire_delay){
			Rigidbody projectile;
			projectile = Instantiate(rocket, transform.position + transform.forward*0.5f, transform.rotation);
			projectile.velocity = transform.TransformDirection(Vector3.forward * projectile_speed) + player_velocity;
			Debug.Log("Projectile velocity: " + projectile.velocity.ToString() + " Player velocity: " + player_velocity.ToString());
			last_fired = Time.time;
		}	
	}

	void FixedUpdate() {
		player_velocity = (player.transform.position - last_pos) / Time.fixedDeltaTime;
		last_pos = player.transform.position;
	}
}
