using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof (CharacterController))]
    [RequireComponent(typeof (AudioSource))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private bool m_IsWalking;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_CrouchSpeed;
        [SerializeField] private float m_CrouchHeightMultiplier;
        [SerializeField] private float m_GroundPoundInitalVelocity;
        [SerializeField] private float m_SlideFriction;
        [SerializeField] private float m_SlideJumpBoostSpeed;
        [SerializeField] private float m_RunSpeed;
        [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private float m_airAccelerate = 30.0f;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private MouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField] private float m_StepInterval;
        [SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
        [SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
        [SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.

        private Camera m_Camera;
        private bool m_Crouch;
        private bool m_IsCrouching;
        private float m_OriginalHeight;
        private bool m_IsSliding;
        private bool m_Jump;
        private bool m_DoubleJump;
        private bool m_DoubleJumpAvailable;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private Vector3 m_CenterCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping;
        private AudioSource m_AudioSource;

        // Use this for initialization
        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle/2f;
            m_Jumping = false;
            m_AudioSource = GetComponent<AudioSource>();
			m_MouseLook.Init(transform , m_Camera.transform);
            m_DoubleJumpAvailable = true;
            m_OriginalHeight = m_CharacterController.height;
        }


        // Update is called once per frame
        private void Update()
        {
            RotateView();
            
            // the jump state needs to read here to make sure it is not missed
            m_Jump = CrossPlatformInputManager.GetButton("Jump");

            if(m_Jumping || !m_CharacterController.isGrounded)
            {
                m_DoubleJump = CrossPlatformInputManager.GetButtonDown("Jump");
            }

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();

                // This value was changed from the default 0f
                // it fixes a bug where the landing sound would play multiple times when sliding from a jump
                m_MoveDir.y = -m_StickToGroundForce;
                m_Jumping = false;
                m_DoubleJumpAvailable = true;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            UpdateCrouching();
            
            m_PreviouslyGrounded = m_CharacterController.isGrounded;
        }

        private void UpdateCrouching()
        {
            if(m_Crouch)    // Crouch button is pressed
            {
                if(!m_IsCrouching)
                {
                    m_IsCrouching = true;
                    m_CharacterController.height = m_OriginalHeight * m_CrouchHeightMultiplier;
                    float heightDiff = m_OriginalHeight - m_CharacterController.height;
                    transform.position -= new Vector3(0, heightDiff/2, 0);
                    m_CenterCameraPosition = m_Camera.transform.localPosition;
                }
                m_Camera.transform.localPosition = Vector3.Lerp(m_Camera.transform.localPosition, new Vector3(0, 0.2f, 0), 10*Time.deltaTime);
            }
            else if(m_IsCrouching && !m_Crouch) // Crouching button is let go and the player was crouching
            {   
                // Only unable to uncrouch if there is room above the player
                RaycastHit hit;
                bool ceilingAbove = Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.up, out hit,
                                0.9f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                                // The value of 2f is arbitrary, need to find a better value
                if(!ceilingAbove)
                {
                    m_IsCrouching = false;
                    m_CharacterController.height = m_OriginalHeight;
                    float heightDiff = m_OriginalHeight - m_CharacterController.height;
                    transform.position += new Vector3(0, heightDiff/2, 0);
                    m_CenterCameraPosition = m_Camera.transform.localPosition;
                }
            }
            else
            {   
                // Interpolate camera to orignal position
                m_Camera.transform.localPosition = Vector3.Lerp(m_Camera.transform.localPosition, m_OriginalCameraPosition, 10*Time.deltaTime);
            }
        }


        private void PlayLandingSound()
        {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }


        private void FixedUpdate()
        {
            float speed;
            GetInput(out speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward*m_Input.y + transform.right*m_Input.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height/2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            // Get relative ground speed for vector calculations and comparisons
            Vector3 XZGroundDir = new Vector3(m_MoveDir.x, 0, m_MoveDir.z);
            float trueGroundSpeed = Vector3.ProjectOnPlane(XZGroundDir, hitInfo.normal).magnitude;

            if (m_CharacterController.isGrounded)
            {
                if (m_Jump)
                {
                    m_MoveDir.y = m_JumpSpeed;
                    PlayJumpSound();
                    m_Jump = false;
                    m_Jumping = true;

                    // Give a speed boost along x-z plane
                    if(m_IsSliding)
                    {   
                        if (trueGroundSpeed < m_SlideJumpBoostSpeed){
                            XZGroundDir = XZGroundDir.normalized * m_SlideJumpBoostSpeed;
                        }
                        m_MoveDir.x = XZGroundDir.x;
                        m_MoveDir.z = XZGroundDir.z;
                        m_IsSliding = false;
                    }
                }
                else if ((!m_IsWalking || m_IsSliding) && m_Crouch && m_IsCrouching && trueGroundSpeed - 0.1f > m_CrouchSpeed)  // Sliding
                {
                    m_IsSliding = true;

                    // Find downwards direction of slope, take x and z components of normal and project it onto the plane denoted by normal of slope
                    Vector3 slopeXZDirection = new Vector3(hitInfo.normal.x, 0, hitInfo.normal.z);
                    Vector3 slopeGradient = Vector3.ProjectOnPlane(slopeXZDirection, hitInfo.normal).normalized;

                    Vector3 gravitySlopeForce = Vector3.Project(Physics.gravity, slopeGradient);
                    Vector3 gravityNormalForce = Vector3.Project(Physics.gravity, -hitInfo.normal);
                    
                    Vector3 accelerationDueToSlope = gravitySlopeForce;
                    Vector3 accelerationDueToFriction = -m_MoveDir.normalized * gravityNormalForce.magnitude * m_SlideFriction;

                    m_MoveDir += (accelerationDueToSlope + accelerationDueToFriction) * Time.fixedDeltaTime;

                    // Give player some influence in direction, but not add to sliding
                    Vector3 playerInfluence = Vector3.Project(desiredMove, Vector3.Cross(m_MoveDir, Vector3.up));
                    float oldMagnitude = m_MoveDir.magnitude;
                    m_MoveDir += playerInfluence * 0.2f; 
                    m_MoveDir = m_MoveDir.normalized * oldMagnitude;
                }
                else
                {
                    m_IsSliding = false;

                    m_MoveDir.x = desiredMove.x*speed;
                    m_MoveDir.z = desiredMove.z*speed;
                    m_MoveDir.y = -m_StickToGroundForce;
                }
            }
            else
            {   
                if(m_DoubleJumpAvailable && m_DoubleJump)  // Double Jump
                {
                    // Adjust x and z movement speed based off of input
                    // Clamp it, if they are already moving faster in that direction dont update, else update
                    float desiredX = desiredMove.x * speed;
                    float desiredZ = desiredMove.z * speed;

                    // If signs disagree or if absvalue of desired movement greater than currnet movement
                    if (m_MoveDir.x * desiredX < 0 || Math.Abs(desiredX) > Math.Abs(m_MoveDir.x)) 
                    {
                        m_MoveDir.x = desiredX;
                    }

                    if (m_MoveDir.z * desiredZ < 0 || Math.Abs(desiredZ) > Math.Abs(m_MoveDir.z))
                    {
                        m_MoveDir.z = desiredZ;
                    }

                    m_MoveDir.y = m_JumpSpeed;
                    PlayJumpSound();
                    m_DoubleJumpAvailable = false;
                }
                else
                {   
                    // Ground pound
                    if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl))
                    {
                        if(m_MoveDir.y > m_GroundPoundInitalVelocity)
                        {
                            m_MoveDir.y = m_GroundPoundInitalVelocity;
                        }
                    }

                    // Air Accelerate - Based off of Source engine air control
                    // https://github.com/ValveSoftware/source-sdk-2013/blob/master/mp/src/game/shared/gamemovement.cpp#L1707

                    Vector3 desiredDir = desiredMove.normalized;

                    float currentSpeed = Vector3.Dot(desiredDir, m_MoveDir);

                    float addSpeed = speed - currentSpeed;

                    if (addSpeed > 0)
                    {
                        float accelSpeed = speed * m_airAccelerate * Time.fixedDeltaTime;
                        
                        if (accelSpeed > addSpeed) accelSpeed = addSpeed;
                        
                        m_MoveDir += desiredDir * accelSpeed;
                    }
                    
                    m_MoveDir += Physics.gravity*m_GravityMultiplier*Time.fixedDeltaTime;
                }
            }
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir*Time.fixedDeltaTime);
            
            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);

            m_MouseLook.UpdateCursorLock();
        }


        private void PlayJumpSound()
        {
            m_AudioSource.clip = m_JumpSound;
            m_AudioSource.Play();
        }


        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                float cycleMultiplier = speed*(m_IsWalking ? 1f : m_RunstepLenghten);
                if (m_IsCrouching) cycleMultiplier = speed * 2f;
                m_StepCycle += (m_CharacterController.velocity.magnitude + cycleMultiplier)*
                             Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }


        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded || m_IsSliding)
            {
                return;
            }
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_FootstepSounds.Length);
            m_AudioSource.clip = m_FootstepSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_AudioSource.clip;
        }


        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob)
            {
                return;
            }
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                      (speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_CenterCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }


        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");

            bool waswalking = m_IsWalking;

            // Check if player is crouching
            m_Crouch = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);

#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
#endif  

            // Default movement speed for airborne calculations
            speed = m_WalkSpeed;

            // Modify speed based off of input
            if(m_CharacterController.isGrounded)
            {
                // set the desired speed to be walking, running or crouching
                speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
                speed = m_IsCrouching ? m_CrouchSpeed : speed;
            }

            m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }
        }


        private void RotateView()
        {
            m_MouseLook.LookRotation (transform, m_Camera.transform);
        }


        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;

            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }

            
            body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
        }
    }
}
