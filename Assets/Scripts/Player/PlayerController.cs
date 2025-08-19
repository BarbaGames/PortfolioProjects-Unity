using Agents.SecurityAgents;
using UnityEngine;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float sprintSpeed = 6f;
        [SerializeField] private float crouchSpeed = 1.5f;
        [SerializeField] private float crouchHeight = 0.5f;

        [Header("Detection Settings")] 
        [SerializeField] private float walkNoiseRadius = 5f;
        [SerializeField] private float crouchNoiseRadius = 2.5f;
        [SerializeField] private float sprintNoiseRadius = 10f;

        private CharacterController _characterController;
        private Vector3 _originalScale;
        private bool _isCrouching = false;
        private bool _isSprinting = false;
        private Vector3 _velocity;
        private float _currentSpeed;
        private float _currentNoiseRadius;

        public bool IsMoving { get; private set; }
        public bool IsCrouching => _isCrouching;
        public bool IsSprinting => _isSprinting;
        public float CurrentNoiseRadius => _currentNoiseRadius;
        public Vector3 Position => transform.position;

        private void Start()
        {
            _characterController = GetComponent<CharacterController>();
            _originalScale = transform.localScale;
            UpdateMovementState();
        }

        private void Update()
        {
            HandleInput();
            HandleMovement();
            UpdateNoiseRadius();

            // Notify SecurityManager about player noise
            if (IsMoving)
            {
                SecurityManager.Instance?.OnPlayerNoise(Position, _currentNoiseRadius);
            }
        }

        private void HandleInput()
        {
            // Crouch input
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                _isCrouching = !_isCrouching;
                UpdateCrouchState();
            }

            // Sprint input
            _isSprinting = Input.GetKey(KeyCode.LeftShift) && !_isCrouching;

            UpdateMovementState();
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
            IsMoving = direction.magnitude > 0.1f;

            if (IsMoving)
            {
                Vector3 move = transform.TransformDirection(direction) * _currentSpeed;
                _velocity.x = move.x;
                _velocity.z = move.z;
            }
            else
            {
                _velocity.x = 0f;
                _velocity.z = 0f;
            }

            // Apply gravity
            if (_characterController.isGrounded)
            {
                _velocity.y = -2f;
            }
            else
            {
                _velocity.y += Physics.gravity.y * Time.deltaTime;
            }

            _characterController.Move(_velocity * Time.deltaTime);
        }

        private void UpdateMovementState()
        {
            if (_isCrouching)
            {
                _currentSpeed = crouchSpeed;
            }
            else if (_isSprinting)
            {
                _currentSpeed = sprintSpeed;
            }
            else
            {
                _currentSpeed = walkSpeed;
            }
        }

        private void UpdateCrouchState()
        {
            if (_isCrouching)
            {
                transform.localScale = new Vector3(_originalScale.x, _originalScale.y * crouchHeight, _originalScale.z);
            }
            else
            {
                transform.localScale = _originalScale;
            }
        }

        private void UpdateNoiseRadius()
        {
            if (!IsMoving)
            {
                _currentNoiseRadius = 0f;
                return;
            }

            if (_isCrouching)
            {
                _currentNoiseRadius = crouchNoiseRadius;
            }
            else if (_isSprinting)
            {
                _currentNoiseRadius = sprintNoiseRadius;
            }
            else
            {
                _currentNoiseRadius = walkNoiseRadius;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (IsMoving)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, _currentNoiseRadius);
            }
        }
    }
}