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
        private bool _isCrouching;
        private bool _isSprinting;
        private Vector3 _velocity;
        private float _currentSpeed;
        private float _currentNoiseRadius;

        public bool IsMoving { get; private set; }
        public bool IsCrouching => _isCrouching;
        public bool IsSprinting => _isSprinting;
        public float CurrentNoiseRadius => _currentNoiseRadius;
        public Vector3 Position => transform.position;

        [Header("Debug & Quality of Life")] 
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showMovementGizmos = true;
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

        [Header("Audio Feedback")] 
        [SerializeField] private AudioClip walkSound;
        [SerializeField] private AudioClip sprintSound;
        [SerializeField] private AudioClip crouchSound;

        private AudioSource _audioSource;
        private Vector3 _lastNoisePosition;
        private float _noiseTimer = 0f;
        private const float NoiseInterval = 0.5f;

        // Movement state tracking
        private float _movementStateChangeTime;
        private bool _wasMoving;

        private void Start()
        {
            _characterController = GetComponent<CharacterController>();
            _audioSource = GetComponent<AudioSource>();

            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _originalScale = transform.localScale;
            UpdateMovementState();

            if (showDebugInfo)
                Debug.Log("Player Controller initialized");
        }

        private void Update()
        {
            HandleInput();
            HandleMovement();
            UpdateNoiseRadius();
            HandleAudioFeedback();
            NotifySecurityManager();
            UpdateDebugInfo();
        }

        private void HandleInput()
        {
            // More flexible input handling
            bool crouchInput = Input.GetKey(crouchKey);
            bool sprintInput = Input.GetKey(sprintKey);

            // Handle crouch toggle vs hold
            if (Input.GetKeyDown(crouchKey))
            {
                _isCrouching = !_isCrouching;
                _movementStateChangeTime = Time.time;

                if (showDebugInfo)
                    Debug.Log($"Crouch {(_isCrouching ? "enabled" : "disabled")}");
            }

            // Sprint only works when not crouching
            bool wasSprinting = _isSprinting;
            _isSprinting = sprintInput && !_isCrouching;

            if (_isSprinting != wasSprinting)
            {
                _movementStateChangeTime = Time.time;
                if (showDebugInfo)
                    Debug.Log($"Sprint {(_isSprinting ? "enabled" : "disabled")}");
            }

            UpdateMovementState();
        }

        private void HandleAudioFeedback()
        {
            if (!_audioSource || !IsMoving) return;

            AudioClip soundToPlay = null;

            if (_isCrouching && crouchSound)
                soundToPlay = crouchSound;
            else if (_isSprinting && sprintSound)
                soundToPlay = sprintSound;
            else if (walkSound)
                soundToPlay = walkSound;

            if (soundToPlay && !_audioSource.isPlaying)
            {
                _audioSource.clip = soundToPlay;
                _audioSource.volume = _isCrouching ? 0.3f : (_isSprinting ? 0.8f : 0.5f);
                _audioSource.Play();
            }
        }

        private void NotifySecurityManager()
        {
            // Optimize noise notifications
            _noiseTimer += Time.deltaTime;

            if (IsMoving && _noiseTimer >= NoiseInterval)
            {
                if (Vector3.Distance(Position, _lastNoisePosition) > 1f)
                {
                    SecurityManager.Instance?.OnPlayerNoise(Position, _currentNoiseRadius);
                    _lastNoisePosition = Position;
                    _noiseTimer = 0f;
                }
            }
        }

        private void UpdateDebugInfo()
        {
            if (!showDebugInfo) return;

            // Track movement state changes
            if (IsMoving != _wasMoving)
            {
                Debug.Log($"Player movement: {(IsMoving ? "started" : "stopped")} " +
                          $"(Speed: {_currentSpeed:F1}, Noise: {_currentNoiseRadius:F1})");
                _wasMoving = IsMoving;
            }
        }

        // Enhanced movement state info
        public string GetMovementStateInfo()
        {
            string state = "Standing";
            if (IsMoving)
            {
                if (_isCrouching) state = "Crouching";
                else if (_isSprinting) state = "Sprinting";
                else state = "Walking";
            }

            return $"{state} (Speed: {_currentSpeed:F1}, Noise: {_currentNoiseRadius:F1})";
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
            if (!showMovementGizmos) return;

            // Show noise radius
            if (IsMoving)
            {
                Gizmos.color = _isCrouching ? Color.green : (_isSprinting ? Color.red : Color.yellow);
                Gizmos.DrawWireSphere(transform.position, _currentNoiseRadius);

                // Show movement direction
                Vector3 moveDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
                if (moveDir.magnitude > 0.1f)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(transform.position, moveDir.normalized * 2f);
                }
            }
        }

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 150));
            GUILayout.Label("=== Player Debug ===");
            GUILayout.Label($"State: {GetMovementStateInfo()}");
            GUILayout.Label($"Position: {Position}");
            GUILayout.Label($"Grounded: {_characterController.isGrounded}");

            GUILayout.Label("Controls:");
            GUILayout.Label($"Crouch: {crouchKey}");
            GUILayout.Label($"Sprint: {sprintKey}");

            GUILayout.EndArea();
        }
    }
}