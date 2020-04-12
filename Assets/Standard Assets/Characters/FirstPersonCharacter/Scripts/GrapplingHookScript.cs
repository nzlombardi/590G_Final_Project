using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

namespace GrapplingHook
{
	public class GrapplingHookScript : MonoBehaviour {

		public float hookVelocity;
		public float maxHookDistance;
		public float minHookDistance;
		public Rigidbody hookPreset;
		public Rigidbody currentHook;
		private HookScript currentHookScript;

		public Camera playerCamera;
		public FirstPersonController playerController;

		void Start () {
			playerController = GetComponent<FirstPersonController>();
		}
		
		// Update is called once per frame
		void Update () {
			if(currentHook == null)
			{
				if(Input.GetButtonDown("Fire2"))
				{
					// Create instance of hook
					Vector3 playerLookDirection = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f)).direction;
					currentHook = 
						Instantiate(hookPreset, playerController.transform.position + playerLookDirection*playerController.height/2.0f, Quaternion.Euler(playerLookDirection));
					currentHook.velocity = playerLookDirection * hookVelocity;
					currentHookScript = currentHook.GetComponent<HookScript>();
				}
			}
			else
			{
				// There is a currently in-play hook
				Vector3 playerLookDirection = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f)).direction;
				Vector3 playerToGrappleDirection = (currentHook.position - playerCamera.transform.position).normalized;
				float currentHookDistance = Vector3.Distance(playerController.transform.position, currentHook.position);
				if (Input.GetButtonDown("Fire2") || currentHookDistance > maxHookDistance 
				|| (currentHookScript.hooked &&  currentHookDistance < minHookDistance)
				|| Vector3.Dot(playerLookDirection, playerToGrappleDirection) < 0)
				{
					DestroyHook();
				}
			}
		}

		public void DestroyHook(){
			if (currentHook != null)
			{
				Destroy(currentHook.gameObject);
				currentHook = null;
				playerController.m_IsGrappling = false;
			}
		}
	}
}
