using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.PPM.XRConference
{
    public class MobileInputController : MonoBehaviour
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

        // Start is called before the first frame update
        void Start()
        {
            characterController = GetComponent<CharacterController>();

            animator = GetComponent<Animator>();
            if (!animator)
            {
                Debug.LogError("PlayerAnimatorManager is Missing Animator Component", this);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if(FixedButton1.Pressed)
            {
                animator.SetTrigger("Jump");
            }

            if (FixedButton2.Pressed)
            {
                animator.SetTrigger("Jump");
            }
            
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
    }
}
