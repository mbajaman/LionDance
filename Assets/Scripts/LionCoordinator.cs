using System;
using UnityEngine;

/// <summary>
/// Requires the front and back players to press the same action (same step
/// direction, or both Jump) within a time window before either half actually
/// acts. Mismatched intents, presses that are too fast or too slow, and
/// timeouts log a debug message and clear the pending state.
///
/// Exposes events and read-only state so a HUD (see <see cref="RhythmHUD"/>)
/// can show the timing window, cooldowns, and per-side success/failure feedback.
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

    /// <summary>Minimum seconds between front and back press (any closer = "too fast" fail).</summary>
    public float MinSyncDelay => minSyncDelay;

    /// <summary>Maximum seconds between front and back press (anything later = timeout fail).</summary>
    public float SyncWindow => syncWindow;

    /// <summary>Seconds the side that just failed must wait before another press is accepted.</summary>
    public float FailureCooldown => failureCooldown;

    /// <summary>Action the front half is currently waiting for the back to confirm (or <see cref="LionAction.None"/>).</summary>
    public LionAction FrontPending => _frontPending;

    /// <summary><see cref="Time.time"/> at which the front's pending press was registered. Only meaningful when <see cref="FrontPending"/>.IsValid.</summary>
    public float FrontPendingTime => _frontPendingTime;

    /// <summary>Action the back half is currently waiting for the front to confirm.</summary>
    public LionAction BackPending => _backPending;

    /// <summary><see cref="Time.time"/> at which the back's pending press was registered.</summary>
    public float BackPendingTime => _backPendingTime;

    /// <summary><see cref="Time.time"/> until which front presses are ignored as cooldown.</summary>
    public float FrontCooldownUntil => _frontCooldownUntil;

    /// <summary><see cref="Time.time"/> until which back presses are ignored as cooldown.</summary>
    public float BackCooldownUntil => _backCooldownUntil;

    /// <summary>
    /// Fires whenever a side's input is received, before resolution. <c>isFront</c> is true for the
    /// front half, false for back. Useful for showing which button was just pressed.
    /// </summary>
    public event Action<bool, LionAction> SidePressed;

    /// <summary>Fires once when both halves successfully agree on an action and it executes.</summary>
    public event Action<LionAction> ActionMatched;

    /// <summary>
    /// Fires whenever a side's press is rejected (cooldown, mismatch, too-fast, timeout).
    /// In the "both fail together" cases (mismatch, too-fast) this fires once for each side.
    /// </summary>
    public event Action<bool> SideFailed;

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
            SideFailed?.Invoke(true);
        }

        if (_backPending.IsValid && Time.time - _backPendingTime > syncWindow)
        {
            if (verboseLogging)
            {
                Debug.Log($"[Lion] Rhythm fail: back pressed {_backPending} but front didn't respond within {syncWindow:0.00}s.");
            }
            _backPending = LionAction.None;
            _backCooldownUntil = Time.time + failureCooldown;
            SideFailed?.Invoke(false);
        }
    }

    private void OnFrontRequested(LionAction action) => OnRequested(isFront: true, action);
    private void OnBackRequested(LionAction action) => OnRequested(isFront: false, action);

    private void OnRequested(bool isFront, LionAction action)
    {
        string sideName = isFront ? "Front" : "Back";

        SidePressed?.Invoke(isFront, action);

        float cooldownUntil = isFront ? _frontCooldownUntil : _backCooldownUntil;
        if (Time.time < cooldownUntil)
        {
            if (verboseLogging)
            {
                float remainingMs = (cooldownUntil - Time.time) * 1000f;
                Debug.Log($"[Lion] {sideName} press ignored ({remainingMs:0}ms cooldown remaining).");
            }
            SideFailed?.Invoke(isFront);
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
                SideFailed?.Invoke(true);
                SideFailed?.Invoke(false);
                return;
            }

            if (partnerPending.Matches(action))
            {
                if (verboseLogging)
                {
                    Debug.Log($"[Lion] Action matched: {action} (delta {delta * 1000f:0}ms).");
                }
                ExecuteAction(action);
                ActionMatched?.Invoke(action);
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
                SideFailed?.Invoke(true);
                SideFailed?.Invoke(false);
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
