using UnityEngine;

/// <summary>
/// Requires the front and back players to press the same direction within a
/// time window before either anchor actually steps. Mismatched directions or
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

    [Tooltip("Maximum seconds between the front and back press for them to count as one coordinated step.")]
    [SerializeField, Min(0f)] private float syncWindow = 0.25f;

    [Tooltip("After a failed attempt (mismatched directions, too-fast pair, or no partner response in time), " +
             "how long that side must wait before another press is accepted. Set 0 to disable.")]
    [SerializeField, Min(0f)] private float failureCooldown = 0.15f;

    [Tooltip("If true, the coordinator disables each controller's auto-step at Start so only matched presses move the lion.")]
    [SerializeField] private bool overrideControllerAutoStart = true;

    [Header("Debug")]
    [Tooltip("If true, log every successful step and every failure.")]
    [SerializeField] private bool verboseLogging = true;

    private Vector3? _frontPending;
    private float _frontPendingTime;

    private Vector3? _backPending;
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
        if (frontController != null) frontController.StepRequested += OnFrontRequested;
        if (backController != null) backController.StepRequested += OnBackRequested;
    }

    private void OnDisable()
    {
        if (frontController != null) frontController.StepRequested -= OnFrontRequested;
        if (backController != null) backController.StepRequested -= OnBackRequested;
    }

    private void Update()
    {
        if (_frontPending.HasValue && Time.time - _frontPendingTime > syncWindow)
        {
            if (verboseLogging)
            {
                Debug.Log($"[Lion] Rhythm fail: front pressed {Format(_frontPending.Value)} but back didn't respond within {syncWindow:0.00}s.");
            }
            _frontPending = null;
            _frontCooldownUntil = Time.time + failureCooldown;
        }

        if (_backPending.HasValue && Time.time - _backPendingTime > syncWindow)
        {
            if (verboseLogging)
            {
                Debug.Log($"[Lion] Rhythm fail: back pressed {Format(_backPending.Value)} but front didn't respond within {syncWindow:0.00}s.");
            }
            _backPending = null;
            _backCooldownUntil = Time.time + failureCooldown;
        }
    }

    private void OnFrontRequested(Vector3 dir) => OnRequested(isFront: true, dir);
    private void OnBackRequested(Vector3 dir) => OnRequested(isFront: false, dir);

    private void OnRequested(bool isFront, Vector3 dir)
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

        Vector3? selfPending = isFront ? _frontPending : _backPending;
        if (selfPending.HasValue)
        {
            if (verboseLogging)
            {
                Debug.Log($"[Lion] {sideName} already pressed {Format(selfPending.Value)} this beat. Spam press ignored.");
            }
            return;
        }

        Vector3? partnerPending = isFront ? _backPending : _frontPending;
        float partnerTime = isFront ? _backPendingTime : _frontPendingTime;

        bool partnerIsLive = partnerPending.HasValue && Time.time - partnerTime <= syncWindow;

        if (partnerIsLive)
        {
            float delta = Time.time - partnerTime;

            if (delta < minSyncDelay)
            {
                if (verboseLogging)
                {
                    Debug.Log($"[Lion] Rhythm fail: presses too close together ({delta * 1000f:0}ms, need >= {minSyncDelay * 1000f:0}ms).");
                }
                ClearPending();
                _frontCooldownUntil = Time.time + failureCooldown;
                _backCooldownUntil = Time.time + failureCooldown;
                return;
            }

            if (partnerPending.Value == dir)
            {
                if (verboseLogging)
                {
                    Debug.Log($"[Lion] Step matched: {Format(dir)} (delta {delta * 1000f:0}ms).");
                }
                ExecuteStep(dir);
            }
            else
            {
                if (verboseLogging)
                {
                    string frontDir = Format(isFront ? dir : partnerPending.Value);
                    string backDir = Format(isFront ? partnerPending.Value : dir);
                    Debug.Log($"[Lion] Rhythm fail: directions don't match. Front={frontDir}, Back={backDir}.");
                }
                ClearPending();
                _frontCooldownUntil = Time.time + failureCooldown;
                _backCooldownUntil = Time.time + failureCooldown;
            }
            return;
        }

        if (isFront)
        {
            _frontPending = dir;
            _frontPendingTime = Time.time;
        }
        else
        {
            _backPending = dir;
            _backPendingTime = Time.time;
        }
    }

    private void ExecuteStep(Vector3 dir)
    {
        ClearPending();
        if (frontController != null) frontController.ExecuteStep(dir);
        if (backController != null) backController.ExecuteStep(dir);
    }

    private void ClearPending()
    {
        _frontPending = null;
        _backPending = null;
    }

    private static string Format(Vector3 v)
    {
        if (v == Vector3.forward) return "Forward (+Z)";
        if (v == Vector3.back) return "Back (-Z)";
        if (v == Vector3.right) return "Right (+X)";
        if (v == Vector3.left) return "Left (-X)";
        return v.ToString();
    }
}
