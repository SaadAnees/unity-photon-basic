using UnityEngine;
using UnityEngine.EventSystems;

using Photon.Pun;

using System.Collections;

namespace Com.PPM.XRConference
{
    /// <summary>
    /// Player manager.
    /// Handles fire Input and Beams.
    /// </summary>
    public class PlayerManager : MonoBehaviourPunCallbacks
    {

        CharacterController characterController;
        private Animator animator;

        public FixedJoystick LeftJoystick;
        public FixedJoystick RightJoystick;
        public FixedButton FixedButton1, FixedButton2;

        protected float CameraAngleY;
        protected float CameraAngleSpeed = 2f;
        protected float CameraPosY;
        protected float CameraPosSpeed = 0.1f;


        #region Public Fields

        [Tooltip("The current Health of our player")]
        public float Health = 1f;

        #endregion

        #region Private Fields

        [Tooltip("The Beams GameObject to control")]
        [SerializeField]
        private GameObject beams;
        //True, when the user is firing
        bool IsFiring;
        #endregion

        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
        /// </summary>
        void Awake()
        {
            if (beams == null)
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> Beams Reference.", this);
            }
            else
            {
                beams.SetActive(false);
            }
        }

        void Start()
        {
            characterController = GetComponent<CharacterController>();

            animator = GetComponent<Animator>();
            if (!animator)
            {
                Debug.LogError("PlayerAnimatorManager is Missing Animator Component", this);
            }
        }

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity on every frame.
        /// </summary>
        void Update()
        {
            if (Health <= 0f)
            {
                GameManager.Instance.LeaveRoom();
            }

            ProcessInputs();
            MobileMovement();
            // trigger Beams active state
            if (beams != null && IsFiring != beams.activeInHierarchy)
            {
                beams.SetActive(IsFiring);
            }
        }

        void MobileMovement()
        {
           
            var input = new Vector3(LeftJoystick.Horizontal, 0, LeftJoystick.Vertical);
            var vel = Quaternion.AngleAxis(CameraAngleY + 180, Vector3.up) * input * 5f;

            characterController.velocity.Set(vel.x, characterController.velocity.y, vel.z); //= new Vector3(vel.x, characterController.velocity.y, vel.z);

            transform.rotation = Quaternion.AngleAxis(CameraAngleY + 180 + Vector3.SignedAngle(Vector3.forward, input.normalized + Vector3.forward * 0.001f, Vector3.up), Vector3.up);


            CameraAngleY += RightJoystick.Horizontal * CameraAngleSpeed;
            Camera.main.transform.position = transform.position + Quaternion.AngleAxis(CameraAngleY, Vector3.up) * new Vector3(0, 3, 4);
            Camera.main.transform.rotation = Quaternion.LookRotation(transform.position + Vector3.up * 2f - Camera.main.transform.position, Vector3.up);

            animator.SetFloat("Speed", input.x * input.x + input.z * input.z);
            animator.SetFloat("Direction", LeftJoystick.Horizontal, CameraAngleSpeed, Time.deltaTime);
        }
        /// <summary>
        /// MonoBehaviour method called when the Collider 'other' enters the trigger.
        /// Affect Health of the Player if the collider is a beam
        /// Note: when jumping and firing at the same, you'll find that the player's own beam intersects with itself
        /// One could move the collider further away to prevent this or check if the beam belongs to the player.
        /// </summary>
        void OnTriggerEnter(Collider other)
        {
            if (!photonView.IsMine)
            {
                return;
            }
            // We are only interested in Beamers
            // we should be using tags but for the sake of distribution, let's simply check by name.
            if (!other.name.Contains("Beam"))
            {
                return;
            }
            Health -= 0.1f;
        }
        /// <summary>
        /// MonoBehaviour method called once per frame for every Collider 'other' that is touching the trigger.
        /// We're going to affect health while the beams are touching the player
        /// </summary>
        /// <param name="other">Other.</param>
        void OnTriggerStay(Collider other)
        {
            // we dont' do anything if we are not the local player.
            if (!photonView.IsMine)
            {
                return;
            }
            // We are only interested in Beamers
            // we should be using tags but for the sake of distribution, let's simply check by name.
            if (!other.name.Contains("Beam"))
            {
                return;
            }
            // we slowly affect health when beam is constantly hitting us, so player has to move to prevent death.
            Health -= 0.1f * Time.deltaTime;
        }

        #endregion

        #region Custom

        /// <summary>
        /// Processes the inputs. Maintain a flag representing when the user is pressing Fire.
        /// </summary>
        void ProcessInputs()
        {
// PC
#if UNITY_STANDALONE
            if (Input.GetButtonDown("Fire1"))
            {
                if (!IsFiring)
                {
                    IsFiring = true;
                }
            }
            if (Input.GetButtonUp("Fire1"))
            {
                if (IsFiring)
                {
                    IsFiring = false;
                }
            }

#endif
            // Mobile
            if (FixedButton1.Pressed)
            {
                animator.SetTrigger("Jump");
            }

            if (FixedButton2.Pressed)
            {
                if (!IsFiring)
                {
                    IsFiring = true;
                }
            }else
            {
                if (IsFiring)
                {
                    IsFiring = false;
                }
            }


        }

        #endregion
    }
}