using UnityEngine;

/// <summary>
/// Requires the front and back players to press the same action (same step
/// direction, or both Jump) within a time window before either half actually
/// acts. Mismatched intents, presses that are too fast or too slow, and
/// timeouts log a debug message and clear the pending state.
/// </summary>
[DisallowMultipleComponent]
public class LionCoordinator : MonoBehaviour
{
    [Header("Players")]
    [SerializeField] private PlayerController frontController;
    [SerializeField] private PlayerController backController;

    [Header("Rhythm")]
    [Tooltip("Minimum seconds between the front and back press. Presses closer than this are " +
             "treated as simultaneous mashing and fail. Set 0 to allow same-frame presses.")]
    [SerializeField, Min(0f)] private float minSyncDelay = 0.1f;

    [Tooltip("Maximum seconds between the front and back press for them to count as one coordinated action.")]
    [SerializeField, Min(0f)] private float syncWindow = 0.25f;

    [Tooltip("After a failed attempt (mismatched actions, too-fast pair, or no partner response in time), " +
             "how long that side must wait before another press is accepted. Set 0 to disable.")]
    [SerializeField, Min(0f)] private float failureCooldown = 0.15f;

    [Tooltip("If true, the coordinator disables each controller's auto-start at Start so only matched presses act.")]
    [SerializeField] private bool overrideControllerAutoStart = true;

    [Header("Debug")]
    [Tooltip("If true, log every successful action and every failure.")]
    [SerializeField] private bool verboseLogging = true;

    private LionAction _frontPending = LionAction.None;
    private float _frontPendingTime;

    private LionAction _backPending = LionAction.None;
    private float _backPendingTime;

    private float _frontCooldownUntil;
    private float _backCooldownUntil;

    private void Start()
    {
        if (!overrideControllerAutoStart) return;

        if (frontController != null) frontController.AutoStartOnInput = false;
        if (backController != null) backController.AutoStartOnInput = false;
    }

    private void OnEnable()
    {
        if (frontController != null) frontController.ActionRequested += OnFrontRequested;
        if (backController != null) backController.ActionRequested += OnBackRequested;
    }

    private void OnDisable()
    {
        if (frontController != null) frontController.ActionRequested -= OnFrontRequested;
        if (backController != null) backController.ActionRequested -= OnBackRequested;
    }

    private void Update()
    {
        if (_frontPending.IsValid && Time.time - _frontPendingTime > syncWindow)
        {
            if (verboseLogging)
            {
                Debug.Log($"[Lion] Rhythm fail: front pressed {_frontPending} but back didn't respond within {syncWindow:0.00}s.");
            }
            _frontPending = LionAction.None;
            _frontCooldownUntil = Time.time + failureCooldown;
        }

        if (_backPending.IsValid && Time.time - _backPendingTime > syncWindow)
        {
            if (verboseLogging)
            {
                Debug.Log($"[Lion] Rhythm fail: back pressed {_backPending} but front didn't respond within {syncWindow:0.00}s.");
            }
            _backPending = LionAction.None;
            _backCooldownUntil = Time.time + failureCooldown;
        }
    }

    private void OnFrontRequested(LionAction action) => OnRequested(isFront: true, action);
    private void OnBackRequested(LionAction action) => OnRequested(isFront: false, action);

    private void OnRequested(bool isFront, LionAction action)
    {
        string sideName = isFront ? "Front" : "Back";

        float cooldownUntil = isFront ? _frontCooldownUntil : _backCooldownUntil;
        if (Time.time < cooldownUntil)
        {
            if (verboseLogging)
            {
                float remainingMs = (cooldownUntil - Time.time) * 1000f;
                Debug.Log($"[Lion] {sideName} press ignored ({remainingMs:0}ms cooldown remaining).");
            }
            return;
        }

        LionAction selfPending = isFront ? _frontPending : _backPending;
        if (selfPending.IsValid)
        {
            if (verboseLogging)
            {
                Debug.Log($"[Lion] {sideName} already pressed {selfPending} this beat. Spam press ignored.");
            }
            return;
        }

        LionAction partnerPending = isFront ? _backPending : _frontPending;
        float partnerTime = isFront ? _backPendingTime : _frontPendingTime;

        bool partnerIsLive = partnerPending.IsValid && Time.time - partnerTime <= syncWindow;

        if (partnerIsLive)
        {
            float delta = Time.time - partnerTime;

            if (delta < minSyncDelay)
            {
                if (verboseLogging)
                {
                    Debug.Log($"[Lion] Rhythm fail: presses too close together ({delta * 1000f:0}ms, need >= {minSyncDelay * 1000f:0}ms).");
                }
                FailAndCooldownBoth();
                return;
            }

            if (partnerPending.Matches(action))
            {
                if (verboseLogging)
                {
                    Debug.Log($"[Lion] Action matched: {action} (delta {delta * 1000f:0}ms).");
                }
                ExecuteAction(action);
            }
            else
            {
                if (verboseLogging)
                {
                    LionAction frontAction = isFront ? action : partnerPending;
                    LionAction backAction = isFront ? partnerPending : action;
                    Debug.Log($"[Lion] Rhythm fail: actions don't match. Front={frontAction}, Back={backAction}.");
                }
                FailAndCooldownBoth();
            }
            return;
        }

        if (isFront)
        {
            _frontPending = action;
            _frontPendingTime = Time.time;
        }
        else
        {
            _backPending = action;
            _backPendingTime = Time.time;
        }
    }

    private void ExecuteAction(LionAction action)
    {
        ClearPending();
        if (frontController != null) frontController.ExecuteAction(action);
        if (backController != null) backController.ExecuteAction(action);
    }

    private void FailAndCooldownBoth()
    {
        ClearPending();
        _frontCooldownUntil = Time.time + failureCooldown;
        _backCooldownUntil = Time.time + failureCooldown;
    }

    private void ClearPending()
    {
        _frontPending = LionAction.None;
        _backPending = LionAction.None;
    }
}
