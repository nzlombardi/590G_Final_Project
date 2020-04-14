using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

namespace GrapplingHook
{
	public class HookScript : MonoBehaviour {

		public bool hooked;

		// Use this for initialization
		void Start () {
			hooked = false;
		}
		
		void OnCollisionEnter(Collision target)
		{
			if (target.gameObject.tag == "Player")
			{
				return;
			}

			hooked = true;

			// Freeze hook in place
			GetComponent<Rigidbody>().velocity = Vector3.zero;

			// Set player to grappling state
			GameObject playerGameObject = GameObject.FindWithTag("Player");
			FirstPersonController playerFirstPersonController = playerGameObject.GetComponent<FirstPersonController>();
			playerFirstPersonController.m_IsGrappling = true;
		}
	}
}
