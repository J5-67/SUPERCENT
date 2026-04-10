using UnityEngine;
using UnityEngine.InputSystem;

namespace Supercent.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 720f;

        [Header("Input Settings")]
        [SerializeField] private InputActionReference moveActionReference;

        [Header("Components")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Animator animator;

        private Vector3 _movementDirection;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            if (moveActionReference != null) moveActionReference.action.Enable();
        }

        private void OnDisable()
        {
            if (moveActionReference != null) moveActionReference.action.Disable();
        }

        private void CacheComponents()
        {
            if (characterController == null)
            {
                TryGetComponent(out characterController);
            }
            
            if (animator == null)
            {
                TryGetComponent(out animator);
            }
        }

        private void Update()
        {
            HandleInput();
            HandleRotation();
            HandleMovement();
            UpdateAnimation();
        }

        private void HandleInput()
        {
            if (moveActionReference == null) return;

            Vector2 input = moveActionReference.action.ReadValue<Vector2>();
            _movementDirection = new Vector3(input.x, 0, input.y).normalized;
        }

        private void HandleRotation()
        {
            if (_movementDirection.sqrMagnitude < 0.01f) return;

            Quaternion targetRotation = Quaternion.LookRotation(_movementDirection);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
        }

        private void HandleMovement()
        {
            if (_movementDirection.sqrMagnitude < 0.01f) return;

            characterController.Move(_movementDirection * (moveSpeed * Time.deltaTime));
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;

            float currentSpeed = _movementDirection.sqrMagnitude > 0.01f ? 1f : 0f;
            animator.SetFloat(SpeedHash, currentSpeed);
        }
    }
}
