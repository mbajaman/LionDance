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

    [Tooltip("If true, the controller starts a step immediately on input. " +
             "Set to false when an external system (e.g. LionCoordinator) decides when to step.")]
    [SerializeField] private bool autoStartOnInput = true;

    [Header("Step Movement")]
    [Tooltip("World distance the player moves per button press.")]
    [SerializeField] private float stepDistance = 1f;

    [Tooltip("Seconds it takes to slide one step. Set to 0 to snap instantly.")]
    [SerializeField] private float stepDuration = 0.15f;

    [Header("Rotation")]
    [SerializeField] private bool rotateToMoveDirection = true;

    [Tooltip("Degrees per second the player rotates to face the step direction.")]
    [SerializeField] private float turnSpeed = 720f;

    [Header("Animation")]
    [Tooltip("Optional Animator that plays this half's step clip. Leave null to disable animation.")]
    [SerializeField] private Animator stepAnimator;

    [Tooltip("Trigger parameter on the Animator to fire each time a step starts.")]
    [SerializeField] private string stepTriggerName = "Step";

    public event Action<Vector3> StepRequested;

    public bool IsStepping => _isStepping;

    public bool AutoStartOnInput
    {
        get => autoStartOnInput;
        set => autoStartOnInput = value;
    }

    private InputAction _forwardAction;
    private InputAction _backAction;
    private InputAction _leftAction;
    private InputAction _rightAction;

    private Vector3 _stepStart;
    private Vector3 _stepTarget;
    private float _stepElapsed;
    private bool _isStepping;

    private Quaternion _facingTarget;

    private int _stepTriggerHash;
    private bool _hasStepTrigger;

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

        _stepStart = transform.position;
        _stepTarget = transform.position;
        _facingTarget = transform.rotation;

        _hasStepTrigger = stepAnimator != null && !string.IsNullOrEmpty(stepTriggerName);
        if (_hasStepTrigger) _stepTriggerHash = Animator.StringToHash(stepTriggerName);
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
    }

    private void OnForwardPerformed(InputAction.CallbackContext _) => RequestStep(Vector3.right);
    private void OnBackPerformed(InputAction.CallbackContext _) => RequestStep(Vector3.left);
    private void OnLeftPerformed(InputAction.CallbackContext _) => RequestStep(Vector3.forward);
    private void OnRightPerformed(InputAction.CallbackContext _) => RequestStep(Vector3.back);

    private void RequestStep(Vector3 direction)
    {
        if (_isStepping) return;

        StepRequested?.Invoke(direction);

        if (autoStartOnInput) TryStartStep(direction);
    }

    public void ExecuteStep(Vector3 direction) => TryStartStep(direction);

    private void TryStartStep(Vector3 direction)
    {
        if (_isStepping) return;

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

        if (rotateToMoveDirection)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                _facingTarget,
                turnSpeed * Time.deltaTime);
        }
    }
}
