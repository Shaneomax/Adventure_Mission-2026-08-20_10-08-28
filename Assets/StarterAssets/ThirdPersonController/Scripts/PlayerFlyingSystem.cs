using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(ThirdPersonController))]
    [RequireComponent(typeof(StarterAssetsInputs))]
    public class PlayerFlyingSystem : MonoBehaviour
    {
        [Header("Flying System Settings")]
        public float FlySpeed = 10.0f;
        public float FlyHoldDuration = 0.5f;
        public float MaxFlyDuration = 10f;
        public float MaxFlyHeight = 20f;
        
        [Header("Flying State")]
        public float CurrentFlyTime;
        
        private float _flyHoldTimer;
        
        private ThirdPersonController _thirdPersonController;
        private StarterAssetsInputs _input;
        private CharacterController _controller;
        private Animator _animator;
        private bool _hasAnimator;
        private GameObject _mainCamera;
        
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _animationBlend;

        private int _animIDIsFlying;
        private int _animIDSpeed;
        private int _animIDMotionSpeed;

        private void Start()
        {
            _thirdPersonController = GetComponent<ThirdPersonController>();
            _input = GetComponent<StarterAssetsInputs>();
            _controller = GetComponent<CharacterController>();
            _hasAnimator = TryGetComponent(out _animator);
            
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
            
            _animIDIsFlying = Animator.StringToHash("IsFlying");
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            HandleFlyingToggle();

            if (_thirdPersonController.IsFlying)
            {
                HandleFlyTimer();
                FlyMovement();
            }
        }

        private void HandleFlyingToggle()
        {
            if (_input.fly)
            {
                _flyHoldTimer += Time.deltaTime;
                if (_flyHoldTimer >= FlyHoldDuration)
                {
                    ToggleFlying(!_thirdPersonController.IsFlying);
                    _flyHoldTimer = 0f;
                    _input.fly = false; // Consume input so it doesn't toggle repeatedly
                }
            }
            else
            {
                _flyHoldTimer = 0f;
            }
        }

        private void ToggleFlying(bool flyState)
        {
            _thirdPersonController.IsFlying = flyState;
            
            if (flyState)
            {
                CurrentFlyTime = MaxFlyDuration;
                if (_hasAnimator) _animator.SetBool(_animIDIsFlying, true);
            }
            else
            {
                if (_hasAnimator) _animator.SetBool(_animIDIsFlying, false);
            }
        }

        private void HandleFlyTimer()
        {
            CurrentFlyTime -= Time.deltaTime;
            if (CurrentFlyTime <= 0f)
            {
                ToggleFlying(false);
            }
        }

        private void FlyMovement()
        {
            // WASD Movement similar to ThirdPersonController
            float targetSpeed = _input.move == Vector2.zero ? 0.0f : FlySpeed;

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            float _speed = targetSpeed;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * 10f);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * 10f);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
            
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, 0.12f);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // Apply max height constraint
            Vector3 movement = targetDirection.normalized * (_speed * Time.deltaTime);
            if (transform.position.y > MaxFlyHeight)
            {
                // Force down if above max height
                movement.y = -2f * Time.deltaTime;
            }
            else
            {
                // To maintain altitude while flying
                movement.y = 0f;
            }

            _controller.Move(movement);

            if (_hasAnimator)
            {
                // We set speed so that running/moving animations can play if desired,
                // but if using a single idle flying animation, you might want these to be 0 or separate.
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }
    }
}
