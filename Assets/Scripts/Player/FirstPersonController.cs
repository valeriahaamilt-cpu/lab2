using UnityEngine;

namespace ProjectBreachpoint
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("Look")]
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private float mouseSensitivity = 2.1f;
        [SerializeField] private bool invertY;

        [Header("Movement")]
        [SerializeField] private float runSpeed = 5.2f;
        [SerializeField] private float walkSpeed = 2.6f;
        [SerializeField] private float crouchSpeed = 2.1f;
        [SerializeField] private float acceleration = 42f;
        [SerializeField] private float friction = 18f;
        [SerializeField] private float jumpForce = 5.2f;
        [SerializeField] private float gravity = -18f;

        private CharacterController controller;
        private PlayerHealth health;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float yaw;
        private float pitch;
        private float landingPenalty;
        private bool inputBlocked;
        private bool wasGrounded;

        public Transform CameraRoot
        {
            get { return cameraRoot; }
            set { cameraRoot = value; }
        }

        public Vector3 Velocity
        {
            get { return horizontalVelocity + Vector3.up * verticalVelocity; }
        }

        public bool IsGrounded
        {
            get { return controller != null && controller.isGrounded; }
        }

        public bool IsCrouching { get; private set; }
        public MovementState CurrentState { get; private set; }

        public float AccuracyMovementPenalty
        {
            get
            {
                float speedPenalty = Mathf.InverseLerp(0.4f, runSpeed, horizontalVelocity.magnitude);
                float airPenalty = IsGrounded ? 0f : 1f;
                float crouchBonus = IsCrouching ? -0.25f : 0f;
                return Mathf.Clamp01(speedPenalty + airPenalty + landingPenalty + crouchBonus);
            }
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<PlayerHealth>();
            if (cameraRoot == null)
            {
                Camera camera = GetComponentInChildren<Camera>();
                if (camera != null)
                {
                    cameraRoot = camera.transform;
                }
            }

            yaw = transform.eulerAngles.y;
        }

        private void Update()
        {
            if (health != null && !health.IsAlive)
            {
                return;
            }

            if (!inputBlocked)
            {
                HandleLook();
                HandleMovement();
            }
            else
            {
                ApplyFrictionOnly();
            }

            landingPenalty = Mathf.MoveTowards(landingPenalty, 0f, Time.deltaTime * 3.5f);
        }

        public void SetInputBlocked(bool blocked)
        {
            inputBlocked = blocked;
        }

        public void AddViewKick(float verticalDegrees, float horizontalDegrees)
        {
            pitch -= verticalDegrees;
            yaw += horizontalDegrees;
            pitch = Mathf.Clamp(pitch, -88f, 88f);
            ApplyLookRotation();
        }

        private void HandleLook()
        {
            yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float y = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
            pitch += invertY ? y : -y;
            pitch = Mathf.Clamp(pitch, -88f, 88f);
            ApplyLookRotation();
        }

        private void ApplyLookRotation()
        {
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cameraRoot != null)
            {
                cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            bool grounded = controller.isGrounded;
            if (grounded && !wasGrounded)
            {
                landingPenalty = 0.45f;
            }

            wasGrounded = grounded;

            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            Vector3 input = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f);

            IsCrouching = Input.GetKey(KeyCode.LeftControl);
            bool walking = Input.GetKey(KeyCode.LeftShift);
            float targetSpeed = IsCrouching ? crouchSpeed : walking ? walkSpeed : runSpeed;
            Vector3 targetVelocity = input * targetSpeed;

            float accel = input.sqrMagnitude > 0.01f ? acceleration : friction;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, accel * Time.deltaTime);

            if (grounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (grounded && Input.GetKeyDown(KeyCode.Space) && !IsCrouching)
            {
                verticalVelocity = jumpForce;
            }

            verticalVelocity += gravity * Time.deltaTime;
            controller.height = Mathf.Lerp(controller.height, IsCrouching ? 1.25f : 1.8f, Time.deltaTime * 12f);
            controller.center = Vector3.up * (controller.height * 0.5f);
            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (!grounded)
            {
                CurrentState = MovementState.Airborne;
            }
            else if (IsCrouching)
            {
                CurrentState = MovementState.Crouching;
            }
            else if (horizontalVelocity.magnitude > walkSpeed + 0.2f)
            {
                CurrentState = MovementState.Running;
            }
            else if (horizontalVelocity.magnitude > 0.2f)
            {
                CurrentState = MovementState.Walking;
            }
            else
            {
                CurrentState = MovementState.Standing;
            }
        }

        private void ApplyFrictionOnly()
        {
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, friction * Time.deltaTime);
            verticalVelocity += gravity * Time.deltaTime;
            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
        }
    }
}
