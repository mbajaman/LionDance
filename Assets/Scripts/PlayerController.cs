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

    [Header("Movement")]
    [Tooltip("Units per second the player moves at full input.")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("If true, the player faces the direction it is moving in.")]
    [SerializeField] private bool rotateToMoveDirection = true;

    [Tooltip("Degrees per second the player rotates to face the move direction.")]
    [SerializeField] private float turnSpeed = 720f;

    private InputAction _forwardAction;
    private InputAction _backAction;
    private InputAction _leftAction;
    private InputAction _rightAction;

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
    }

    private void OnEnable()
    {
        _forwardAction?.Enable();
        _backAction?.Enable();
        _leftAction?.Enable();
        _rightAction?.Enable();
    }

    private void OnDisable()
    {
        _forwardAction?.Disable();
        _backAction?.Disable();
        _leftAction?.Disable();
        _rightAction?.Disable();
    }

    private void Update()
    {
        Vector2 input = ReadMoveInput();
        Vector3 move = new Vector3(input.x, 0f, input.y);

        transform.Translate(move * (moveSpeed * Time.deltaTime), Space.World);

        if (rotateToMoveDirection && move.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(move, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                target,
                turnSpeed * Time.deltaTime);
        }
    }

    private Vector2 ReadMoveInput()
    {
        float x = 0f;
        float y = 0f;

        if (_leftAction != null && _leftAction.IsPressed()) y += 1f;
        if (_rightAction != null && _rightAction.IsPressed()) y -= 1f;
        if (_forwardAction != null && _forwardAction.IsPressed()) x += 1f;
        if (_backAction != null && _backAction.IsPressed()) x -= 1f;

        Vector2 v = new Vector2(x, y);
        return v.sqrMagnitude > 1f ? v.normalized : v;
    }
}
