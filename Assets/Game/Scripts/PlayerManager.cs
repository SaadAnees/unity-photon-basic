using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Photon.Pun;

using System.Collections;

namespace Com.PPM.XRConference
{
    /// <summary>
    /// Player manager.
    /// Handles fire Input and Beams.
    /// </summary>
    public class PlayerManager : MonoBehaviourPunCallbacks, IPunObservable
    {

        CharacterController characterController;
        private Animator animator;

        public FixedJoystick[] fixedJoysticks;
        public FixedButton[] fixedButtons;
        public FixedJoystick LeftJoystick;
        public FixedJoystick RightJoystick;
        public FixedButton FixedButton1, FixedButton2;

        protected float CameraAngleY;
        protected float CameraAngleSpeed = 2f;
        protected float CameraPosY =3f;
        protected float CameraPosSpeed = 0.1f;


        #region Public Fields

        [Tooltip("The Player's UI GameObject Prefab")]
        [SerializeField]
        public GameObject PlayerUiPrefab;

        public GameObject JoystickPrefab;

        [Tooltip("The current Health of our player")]
        public float Health = 1f;

        [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
        public static GameObject LocalPlayerInstance;

        #endregion

        #region Private Fields

        [Tooltip("The Beams GameObject to control")]
        [SerializeField]
        private GameObject beams;
        //True, when the user is firing
        bool IsFiring;
        GameObject _joystickGo;

        Text enemyName;
        #endregion

        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
        /// </summary>
        void Awake()
        {

            if (this.beams == null)
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> Beams Reference.", this);
            }
            else
            {
               this.beams.SetActive(false);
            }

            // #Important
            // used in GameManager.cs: we keep track of the localPlayer instance to prevent instantiation when levels are synchronized
            if (photonView.IsMine)
            {
                LocalPlayerInstance = this.gameObject;
            }
            // #Critical
            // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
            DontDestroyOnLoad(this.gameObject);


            if(photonView.IsMine)
            {
                if (JoystickPrefab != null)
                {
                    _joystickGo = Instantiate(JoystickPrefab);
                    //_joystickGo.SendMessage("SetJoystick", this, SendMessageOptions.RequireReceiver);
                    LeftJoystick = _joystickGo.transform.GetChild(0).GetComponent<FixedJoystick>();
                    RightJoystick = _joystickGo.transform.GetChild(1).GetComponent<FixedJoystick>();

                    FixedButton1 = _joystickGo.transform.GetChild(2).GetComponent<FixedButton>();
                    FixedButton2 = _joystickGo.transform.GetChild(3).GetComponent<FixedButton>();
                }
                else
                {
                    Debug.LogWarning("<Color=Red><a>Missing</a></Color> JoystickPrefab reference on player Prefab.", this);
                }
            }


            //fixedJoysticks = GameObject.FindObjectsOfType<FixedJoystick>();
            //fixedButtons = GameObject.FindObjectsOfType<FixedButton>();


            //if (JoystickPrefab != null)
            //{
            //    LeftJoystick = _joystickGo.transform.GetChild(0).GetComponent<FixedJoystick>();
            //    RightJoystick = _joystickGo.transform.GetChild(1).GetComponent<FixedJoystick>();

            //    FixedButton1 = _joystickGo.transform.GetChild(2).GetComponent<FixedButton>();
            //    FixedButton2 = _joystickGo.transform.GetChild(3).GetComponent<FixedButton>();
            //}
        }

        void Start()
        {
            if (PlayerUiPrefab != null)
            {
                GameObject _uiGo = Instantiate(PlayerUiPrefab);
                _uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
                print(_uiGo.transform.GetChild(3));
                enemyName = _uiGo.transform.GetChild(3).GetComponent<Text>();

                CameraAngleY = Camera.main.transform.eulerAngles.y; // Set camera angle at start
                transform.rotation = Quaternion.Euler(0, CameraAngleY + 180, 0);
            }
            else
            {
                Debug.LogWarning("<Color=Red><a>Missing</a></Color> PlayerUiPrefab reference on player Prefab.", this);
            }
            
            characterController = GetComponent<CharacterController>();

            animator = GetComponent<Animator>();
            if (!animator)
            {
                Debug.LogError("PlayerAnimatorManager is Missing Animator Component", this);
            }

#if UNITY_5_4_OR_NEWER
            // Unity 5.4 has a new scene management. register a method to call CalledOnLevelWasLoaded.
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
#endif
        }

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity on every frame.
        /// </summary>
        void Update()
        {
            if (photonView.IsMine == false && PhotonNetwork.IsConnected == true)
            {
                return;
            }

          
            if (photonView.IsMine)
            {
                this.ProcessInputs();
                this.MobileMovement();
                if (this.Health <= 0f)
                {
                    GameManager.Instance.LeaveRoom();
                }

            }

            // trigger Beams active state
            if (this.beams != null && this.IsFiring != this.beams.activeInHierarchy)
            {
                this.beams.SetActive(this.IsFiring);
            }
        }

#if !UNITY_5_4_OR_NEWER
/// <summary>See CalledOnLevelWasLoaded. Outdated in Unity 5.4.</summary>
void OnLevelWasLoaded(int level)
{
    this.CalledOnLevelWasLoaded(level);
}
#endif


        void CalledOnLevelWasLoaded(int level)
        {
            // check if we are outside the Arena and if it's the case, spawn around the center of the arena in a safe zone
            if (!Physics.Raycast(transform.position, -Vector3.up, 5f))
            {
                transform.position = new Vector3(0f, 5f, 0f);
            }

            GameObject _uiGo = Instantiate(this.PlayerUiPrefab);
            _uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
        }

        void MobileMovement()
        {
            //Vector3 direction = Vector3.forward * variableJoystick.Vertical + Vector3.right * variableJoystick.Horizontal;
            var input = new Vector3(LeftJoystick.Horizontal, 0, LeftJoystick.Vertical);

            // Check if there is input from the joystick
            if (input.sqrMagnitude > 0.01f) // Avoid very small values to prevent jittering
            {
                // Calculate the velocity based on camera angle
                var vel = Quaternion.AngleAxis(CameraAngleY + 180, Vector3.up) * input * 5f;

                // Update character's velocity
                characterController.velocity.Set(vel.x, characterController.velocity.y, vel.z);

                // Update character's rotation to face the movement direction
                transform.rotation = Quaternion.AngleAxis(
                    CameraAngleY + 180 + Vector3.SignedAngle(Vector3.forward, input.normalized, Vector3.up),
                    Vector3.up
                );
            }

            // Handle camera rotation
            CameraAngleY += RightJoystick.Horizontal * CameraAngleSpeed;
            CameraPosY = Mathf.Clamp(CameraPosY - RightJoystick.Vertical * CameraPosSpeed, 0, 5f);

            if (Camera.main != null && photonView.IsMine)
            {
                Camera.main.transform.position = transform.position + Quaternion.AngleAxis(CameraAngleY, Vector3.up) * new Vector3(0, CameraPosY, 4);
                Camera.main.transform.rotation = Quaternion.LookRotation(
                    transform.position + Vector3.up * 2f - Camera.main.transform.position,
                    Vector3.up
                );
            }

            // Update animations based on input magnitude
            animator.SetFloat("Speed", input.sqrMagnitude); // Speed is proportional to movement input
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
            }
            else
            {
                if (IsFiring)
                {
                    IsFiring = false;
                }
            }


        }

#if UNITY_5_4_OR_NEWER
        void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode loadingMode)
        {
            this.CalledOnLevelWasLoaded(scene.buildIndex);
        }
#endif

#if UNITY_5_4_OR_NEWER
        public override void OnDisable()
        {
            // Always call the base to remove callbacks
            base.OnDisable();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }
#endif

        #region IPunObservable implementation


        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // We own this player: send the others our data
                stream.SendNext(this.IsFiring);
                stream.SendNext(this.Health);
                stream.SendNext(this.enemyName.text);

            }
            else
            {
                // Network player, receive data
                this.IsFiring = (bool)stream.ReceiveNext();
                this.Health = (float)stream.ReceiveNext();
                this.enemyName.text = (string)stream.ReceiveNext();
            }
            
        }


        #endregion

        #endregion
    }
}