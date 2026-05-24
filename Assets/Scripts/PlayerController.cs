using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Drag the PlayerInputSystem.inputactions asset here.")]
    [SerializeField] private InputActionAsset inputActions;

    [Tooltip("Name of the action map inside the InputActionAsset that holds the movement buttons.")]
    [SerializeField] private string actionMapName = "PlayerActions";

    [Tooltip("If true, the controller starts an action immediately on input. " +
             "Set to false when an external system (e.g. LionCoordinator) decides when to act.")]
    [SerializeField] private bool autoStartOnInput = true;

    [Header("Step Movement")]
    [Tooltip("World distance the player moves per button press.")]
    [SerializeField] private float stepDistance = 1f;

    [Tooltip("Seconds it takes to slide one step. Set to 0 to snap instantly.")]
    [SerializeField] private float stepDuration = 0.15f;

    [Header("Jump")]
    [Tooltip("Peak world-space height of a jump arc.")]
    [SerializeField] private float jumpHeight = 1f;

    [Tooltip("Total seconds the jump arc takes (up and back down).")]
    [SerializeField] private float jumpDuration = 0.4f;

    [Header("Rotation")]
    [SerializeField] private bool rotateToMoveDirection = true;

    [Tooltip("Degrees per second the player rotates to face the step direction.")]
    [SerializeField] private float turnSpeed = 720f;

    [Header("Animation")]
    [Tooltip("Optional Animator that plays this half's clips. Leave null to disable animation.")]
    [SerializeField] private Animator stepAnimator;

    [Tooltip("Trigger parameter on the Animator to fire each time a step starts.")]
    [SerializeField] private string stepTriggerName = "Step";

    [Tooltip("Trigger parameter on the Animator to fire each time a jump starts.")]
    [SerializeField] private string jumpTriggerName = "Jump";

    public event Action<LionAction> ActionRequested;

    public bool IsStepping => _isStepping;
    public bool IsJumping => _isJumping;
    public bool IsBusy => _isStepping || _isJumping;

    public bool AutoStartOnInput
    {
        get => autoStartOnInput;
        set => autoStartOnInput = value;
    }

    private InputAction _forwardAction;
    private InputAction _backAction;
    private InputAction _leftAction;
    private InputAction _rightAction;
    private InputAction _jumpAction;

    private Vector3 _stepStart;
    private Vector3 _stepTarget;
    private float _stepElapsed;
    private bool _isStepping;

    private Vector3 _jumpAnchor;
    private float _jumpElapsed;
    private bool _isJumping;

    private Quaternion _facingTarget;

    private int _stepTriggerHash;
    private bool _hasStepTrigger;
    private int _jumpTriggerHash;
    private bool _hasJumpTrigger;

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError($"{nameof(PlayerController)}: No InputActionAsset assigned.", this);
            enabled = false;
            return;
        }

        var map = inputActions.FindActionMap(actionMapName, throwIfNotFound: true);
        _forwardAction = map.FindAction("Forward", throwIfNotFound: true);
        _backAction = map.FindAction("Back", throwIfNotFound: true);
        _leftAction = map.FindAction("Left", throwIfNotFound: true);
        _rightAction = map.FindAction("Right", throwIfNotFound: true);
        _jumpAction = map.FindAction("Jump", throwIfNotFound: false);

        _stepStart = transform.position;
        _stepTarget = transform.position;
        _facingTarget = transform.rotation;

        _hasStepTrigger = stepAnimator != null && !string.IsNullOrEmpty(stepTriggerName);
        if (_hasStepTrigger) _stepTriggerHash = Animator.StringToHash(stepTriggerName);

        _hasJumpTrigger = stepAnimator != null && !string.IsNullOrEmpty(jumpTriggerName);
        if (_hasJumpTrigger) _jumpTriggerHash = Animator.StringToHash(jumpTriggerName);
    }

    private void OnEnable()
    {
        if (_forwardAction == null) return;

        _forwardAction.performed += OnForwardPerformed;
        _backAction.performed += OnBackPerformed;
        _leftAction.performed += OnLeftPerformed;
        _rightAction.performed += OnRightPerformed;

        _forwardAction.Enable();
        _backAction.Enable();
        _leftAction.Enable();
        _rightAction.Enable();

        if (_jumpAction != null)
        {
            _jumpAction.performed += OnJumpPerformed;
            _jumpAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (_forwardAction == null) return;

        _forwardAction.performed -= OnForwardPerformed;
        _backAction.performed -= OnBackPerformed;
        _leftAction.performed -= OnLeftPerformed;
        _rightAction.performed -= OnRightPerformed;

        _forwardAction.Disable();
        _backAction.Disable();
        _leftAction.Disable();
        _rightAction.Disable();

        if (_jumpAction != null)
        {
            _jumpAction.performed -= OnJumpPerformed;
            _jumpAction.Disable();
        }
    }

    private void OnForwardPerformed(InputAction.CallbackContext _) => RequestAction(LionAction.Step(Vector3.right));
    private void OnBackPerformed(InputAction.CallbackContext _) => RequestAction(LionAction.Step(Vector3.left));
    private void OnLeftPerformed(InputAction.CallbackContext _) => RequestAction(LionAction.Step(Vector3.forward));
    private void OnRightPerformed(InputAction.CallbackContext _) => RequestAction(LionAction.Step(Vector3.back));
    private void OnJumpPerformed(InputAction.CallbackContext _) => RequestAction(LionAction.Jump());

    private void RequestAction(LionAction action)
    {
        if (IsBusy) return;

        ActionRequested?.Invoke(action);

        if (autoStartOnInput) ExecuteAction(action);
    }

    public void ExecuteAction(LionAction action)
    {
        switch (action.ActionKind)
        {
            case LionAction.Kind.Step:
                TryStartStep(action.Direction);
                break;
            case LionAction.Kind.Jump:
                TryStartJump();
                break;
        }
    }

    private void TryStartStep(Vector3 direction)
    {
        if (IsBusy) return;

        _stepStart = transform.position;
        _stepTarget = _stepStart + direction * stepDistance;
        _stepElapsed = 0f;
        _isStepping = true;

        if (rotateToMoveDirection)
        {
            _facingTarget = Quaternion.LookRotation(direction, Vector3.up);
        }

        if (_hasStepTrigger) stepAnimator.SetTrigger(_stepTriggerHash);
    }

    private void TryStartJump()
    {
        if (IsBusy) return;

        _jumpAnchor = transform.position;
        _jumpElapsed = 0f;
        _isJumping = true;

        if (_hasJumpTrigger) stepAnimator.SetTrigger(_jumpTriggerHash);
    }

    private void Update()
    {
        if (_isStepping)
        {
            if (stepDuration <= 0f)
            {
                transform.position = _stepTarget;
                _isStepping = false;
            }
            else
            {
                _stepElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(_stepElapsed / stepDuration);
                float eased = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(_stepStart, _stepTarget, eased);

                if (t >= 1f)
                {
                    transform.position = _stepTarget;
                    _isStepping = false;
                }
            }
        }
        else if (_isJumping)
        {
            if (jumpDuration <= 0f)
            {
                transform.position = _jumpAnchor;
                _isJumping = false;
            }
            else
            {
                _jumpElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(_jumpElapsed / jumpDuration);
                float arc = Mathf.Sin(t * Mathf.PI);
                Vector3 pos = _jumpAnchor;
                pos.y += arc * jumpHeight;
                transform.position = pos;

                if (t >= 1f)
                {
                    transform.position = _jumpAnchor;
                    _isJumping = false;
                }
            }
        }

        if (rotateToMoveDirection)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                _facingTarget,
                turnSpeed * Time.deltaTime);
        }
    }
}
