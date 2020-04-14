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
		public GameObject ropePreset;
		public Rigidbody currentHook;
		public GameObject currentRope;
		private HookScript currentHookScript;
		public float grapplePullStrength;
		public float grappleGravityMultiplier;
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

					// Create instance of rope
					currentRope = 
						Instantiate(ropePreset, playerController.transform.position + playerLookDirection*playerController.height/4.0f, Quaternion.Euler(playerLookDirection));
					currentRope.transform.LookAt(currentHook.transform);
					currentRope.transform.eulerAngles = currentRope.transform.eulerAngles + new Vector3(90.0f, 0.0f, 0.0f);	
				}
			}
			else
			{
				// There is a currently in-play hook
				Vector3 playerLookDirection = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f)).direction;
				Vector3 playerToGrappleVector = currentHook.position - playerCamera.transform.position;
				Vector3 playerToGrappleDirection = playerToGrappleVector.normalized;
				float currentHookDistance = Vector3.Distance(playerController.transform.position, currentHook.position);

				// Update rope position and scale relative to hook
				currentRope.transform.position = playerController.transform.position + playerToGrappleDirection * currentHookDistance / 2.0f;
				
				currentRope.transform.LookAt(currentHook.transform);
				currentRope.transform.eulerAngles = currentRope.transform.eulerAngles + new Vector3(90.0f, 0.0f, 0.0f);	

				currentRope.transform.localScale = new Vector3(0.1f, playerToGrappleVector.magnitude / 2.0f, 0.1f);

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
			if (currentRope != null)
			{
				Destroy(currentRope);
				currentRope = null;
			}
		}
	}
}
