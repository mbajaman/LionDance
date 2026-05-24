using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a minimal per-side HUD that visualizes the <see cref="LionCoordinator"/>'s
/// rhythm window for both halves of the lion.
///
/// Per side (Front / Back) the HUD shows three pieces:
/// <list type="bullet">
///   <item><b>Cooldown bar</b> (top) — appears after a failed press and drains as the
///   cooldown ticks down.</item>
///   <item><b>Rhythm bar</b> — fills over <see cref="LionCoordinator.SyncWindow"/> seconds
///   once this side has pressed. A "valid zone" overlay is auto-positioned to mark the
///   range <c>[MinSyncDelay .. SyncWindow]</c> where the partner's matching press scores.
///   A flash overlay glows green on a successful match and red on any failure.</item>
///   <item><b>Button label</b> — flashes a short symbol (← → ↑ ↓ JUMP) for whatever
///   action was just pressed, then fades out.</item>
/// </list>
///
/// Setup: build the UI in a Canvas, then drag the <see cref="Image"/> / <see cref="TMP_Text"/>
/// / <see cref="CanvasGroup"/> references into the inspector. See the comments on
/// <see cref="SideUI"/> for what each field expects.
/// </summary>
[DisallowMultipleComponent]
public class RhythmHUD : MonoBehaviour
{
    [Serializable]
    public class SideUI
    {
        [Tooltip("Optional title label (e.g. \"P1 - FRONT\"). Not driven by code; just set the text in the inspector.")]
        public TMP_Text titleLabel;

        [Header("Rhythm bar")]
        [Tooltip("Filled Image (Image Type = Filled, Fill Method = Horizontal, Origin = Left). " +
                 "Fill amount tracks elapsed time since this side's press, normalized to SyncWindow.")]
        public Image rhythmFill;

        [Tooltip("Optional overlay that marks the valid-press range. Its RectTransform anchors will be " +
                 "auto-set so it spans from (MinSyncDelay / SyncWindow) to 1.0 of the bar width. " +
                 "Place it as a child of the bar's background with the same width as the background.")]
        public RectTransform validZoneRect;

        [Tooltip("Image whose colour is briefly set to the success/fail colour and then faded. " +
                 "Stretch it to cover the rhythm bar.")]
        public Image flashOverlay;

        [Header("Cooldown bar")]
        [Tooltip("Filled Image (Image Type = Filled, Fill Method = Horizontal, Origin = Left). " +
                 "Fill drains from 1 -> 0 over FailureCooldown after this side fails.")]
        public Image cooldownFill;

        [Tooltip("Optional CanvasGroup wrapping the cooldown bar; alpha is set to 0 when no cooldown is active.")]
        public CanvasGroup cooldownGroup;

        [Header("Button press")]
        [Tooltip("Label that shows the short symbol for the last button this side pressed.")]
        public TMP_Text buttonLabel;

        [Tooltip("Optional CanvasGroup wrapping the button label so it can be faded out.")]
        public CanvasGroup buttonGroup;
    }

    [Header("Source")]
    [Tooltip("LionCoordinator to read state from. Auto-found in the scene at OnEnable if left empty.")]
    [SerializeField] private LionCoordinator coordinator;

    [Header("Players")]
    [SerializeField] private SideUI front = new SideUI();
    [SerializeField] private SideUI back = new SideUI();

    [Header("Colors")]
    [Tooltip("Rhythm fill colour while no press is pending.")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.25f);

    [Tooltip("Rhythm fill colour while a press is pending and waiting for the partner.")]
    [SerializeField] private Color pendingColor = new Color(0.6f, 0.85f, 1f, 0.9f);

    [Tooltip("Rhythm fill colour while a press is pending AND the cursor is inside the valid range.")]
    [SerializeField] private Color pendingValidColor = new Color(0.55f, 1f, 0.7f, 1f);

    [Tooltip("Flash colour for a successful matched action.")]
    [SerializeField] private Color successColor = new Color(0.35f, 1f, 0.4f, 1f);

    [Tooltip("Flash colour for any failed press.")]
    [SerializeField] private Color failColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Tooltip("Tint applied to the valid-zone overlay (so the player can see where the good window is).")]
    [SerializeField] private Color validZoneColor = new Color(0.4f, 1f, 0.55f, 0.18f);

    [Tooltip("Cooldown bar colour.")]
    [SerializeField] private Color cooldownColor = new Color(1f, 0.5f, 0.2f, 0.85f);

    [Header("Animation")]
    [Tooltip("Seconds the green/red flash stays at full strength before fading out.")]
    [SerializeField, Min(0f)] private float flashHoldDuration = 0.08f;

    [Tooltip("Seconds the flash takes to fade from full to transparent.")]
    [SerializeField, Min(0.01f)] private float flashFadeDuration = 0.45f;

    [Tooltip("Seconds the button label stays at full opacity after a press.")]
    [SerializeField, Min(0f)] private float buttonHoldDuration = 0.25f;

    [Tooltip("Seconds the button label takes to fade out after the hold.")]
    [SerializeField, Min(0.01f)] private float buttonFadeDuration = 0.5f;

    private readonly SideAnim _frontAnim = new SideAnim();
    private readonly SideAnim _backAnim = new SideAnim();

    private class SideAnim
    {
        public float flashStart = -100f;
        public Color flashColor;
        public bool flashActive;

        public float buttonShownAt = -100f;
    }

    private void Reset()
    {
        if (coordinator == null)
        {
#if UNITY_2023_1_OR_NEWER
            coordinator = FindFirstObjectByType<LionCoordinator>();
#else
            coordinator = FindObjectOfType<LionCoordinator>();
#endif
        }
    }

    private void OnEnable()
    {
        if (coordinator == null)
        {
#if UNITY_2023_1_OR_NEWER
            coordinator = FindFirstObjectByType<LionCoordinator>();
#else
            coordinator = FindObjectOfType<LionCoordinator>();
#endif
        }

        if (coordinator == null)
        {
            Debug.LogWarning($"{nameof(RhythmHUD)}: No LionCoordinator assigned or found in scene.", this);
            return;
        }

        coordinator.SidePressed += HandleSidePressed;
        coordinator.ActionMatched += HandleActionMatched;
        coordinator.SideFailed += HandleSideFailed;

        ApplyValidZone(front);
        ApplyValidZone(back);
        ApplyCooldownColor(front);
        ApplyCooldownColor(back);
        ClearFlash(front, _frontAnim);
        ClearFlash(back, _backAnim);
        SetGroupAlpha(front.buttonGroup, 0f);
        SetGroupAlpha(back.buttonGroup, 0f);
        SetGroupAlpha(front.cooldownGroup, 0f);
        SetGroupAlpha(back.cooldownGroup, 0f);
    }

    private void OnDisable()
    {
        if (coordinator == null) return;

        coordinator.SidePressed -= HandleSidePressed;
        coordinator.ActionMatched -= HandleActionMatched;
        coordinator.SideFailed -= HandleSideFailed;
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        ApplyValidZone(front);
        ApplyValidZone(back);
        ApplyCooldownColor(front);
        ApplyCooldownColor(back);
    }

    private void Update()
    {
        if (coordinator == null) return;

        UpdateSide(front, _frontAnim,
                   coordinator.FrontPending, coordinator.FrontPendingTime,
                   coordinator.FrontCooldownUntil);

        UpdateSide(back, _backAnim,
                   coordinator.BackPending, coordinator.BackPendingTime,
                   coordinator.BackCooldownUntil);
    }

    private void UpdateSide(SideUI ui, SideAnim anim,
                            LionAction pending, float pendingTime,
                            float cooldownUntil)
    {
        UpdateRhythmFill(ui, pending, pendingTime);
        UpdateCooldownFill(ui, cooldownUntil);
        UpdateFlash(ui, anim);
        UpdateButtonFade(ui, anim);
    }

    private void UpdateRhythmFill(SideUI ui, LionAction pending, float pendingTime)
    {
        if (ui.rhythmFill == null) return;

        if (!pending.IsValid || coordinator.SyncWindow <= 0f)
        {
            ui.rhythmFill.fillAmount = 0f;
            ui.rhythmFill.color = idleColor;
            return;
        }

        float elapsed = Time.time - pendingTime;
        float t = Mathf.Clamp01(elapsed / coordinator.SyncWindow);
        ui.rhythmFill.fillAmount = t;

        bool inValidZone = elapsed >= coordinator.MinSyncDelay && elapsed <= coordinator.SyncWindow;
        ui.rhythmFill.color = inValidZone ? pendingValidColor : pendingColor;
    }

    private void UpdateCooldownFill(SideUI ui, float cooldownUntil)
    {
        float remaining = Mathf.Max(0f, cooldownUntil - Time.time);
        bool active = remaining > 0f && coordinator.FailureCooldown > 0f;
        float t = active ? Mathf.Clamp01(remaining / coordinator.FailureCooldown) : 0f;

        if (ui.cooldownFill != null) ui.cooldownFill.fillAmount = t;
        SetGroupAlpha(ui.cooldownGroup, active ? 1f : 0f);
    }

    private void UpdateFlash(SideUI ui, SideAnim anim)
    {
        if (ui.flashOverlay == null) return;
        if (!anim.flashActive) return;

        float since = Time.time - anim.flashStart;
        float total = flashHoldDuration + flashFadeDuration;

        if (since >= total)
        {
            ClearFlash(ui, anim);
            return;
        }

        float alpha;
        if (since <= flashHoldDuration)
        {
            alpha = anim.flashColor.a;
        }
        else
        {
            float fadeT = (since - flashHoldDuration) / flashFadeDuration;
            alpha = Mathf.Lerp(anim.flashColor.a, 0f, fadeT);
        }

        Color c = anim.flashColor;
        c.a = alpha;
        ui.flashOverlay.color = c;
    }

    private void UpdateButtonFade(SideUI ui, SideAnim anim)
    {
        if (ui.buttonGroup == null) return;

        float since = Time.time - anim.buttonShownAt;
        float alpha;
        if (since < buttonHoldDuration) alpha = 1f;
        else if (since < buttonHoldDuration + buttonFadeDuration)
            alpha = 1f - (since - buttonHoldDuration) / buttonFadeDuration;
        else alpha = 0f;

        ui.buttonGroup.alpha = alpha;
    }

    private void HandleSidePressed(bool isFront, LionAction action)
    {
        SideUI ui = isFront ? front : back;
        SideAnim anim = isFront ? _frontAnim : _backAnim;

        if (ui.buttonLabel != null) ui.buttonLabel.text = ShortLabel(action);
        anim.buttonShownAt = Time.time;
        SetGroupAlpha(ui.buttonGroup, 1f);
    }

    private void HandleActionMatched(LionAction action)
    {
        StartFlash(front, _frontAnim, successColor);
        StartFlash(back, _backAnim, successColor);
    }

    private void HandleSideFailed(bool isFront)
    {
        if (isFront) StartFlash(front, _frontAnim, failColor);
        else StartFlash(back, _backAnim, failColor);
    }

    private void StartFlash(SideUI ui, SideAnim anim, Color color)
    {
        anim.flashStart = Time.time;
        anim.flashColor = color;
        anim.flashActive = true;

        if (ui.flashOverlay != null) ui.flashOverlay.color = color;
    }

    private void ClearFlash(SideUI ui, SideAnim anim)
    {
        anim.flashActive = false;
        if (ui.flashOverlay == null) return;
        Color c = anim.flashColor;
        c.a = 0f;
        ui.flashOverlay.color = c;
    }

    private void ApplyValidZone(SideUI ui)
    {
        if (ui.validZoneRect == null) return;
        if (coordinator == null || coordinator.SyncWindow <= 0f) return;

        float minT = Mathf.Clamp01(coordinator.MinSyncDelay / coordinator.SyncWindow);

        Vector2 aMin = ui.validZoneRect.anchorMin;
        Vector2 aMax = ui.validZoneRect.anchorMax;
        ui.validZoneRect.anchorMin = new Vector2(minT, aMin.y);
        ui.validZoneRect.anchorMax = new Vector2(1f, aMax.y);
        ui.validZoneRect.offsetMin = new Vector2(0f, ui.validZoneRect.offsetMin.y);
        ui.validZoneRect.offsetMax = new Vector2(0f, ui.validZoneRect.offsetMax.y);

        var image = ui.validZoneRect.GetComponent<Image>();
        if (image != null) image.color = validZoneColor;
    }

    private void ApplyCooldownColor(SideUI ui)
    {
        if (ui.cooldownFill != null) ui.cooldownFill.color = cooldownColor;
    }

    private static void SetGroupAlpha(CanvasGroup group, float alpha)
    {
        if (group != null) group.alpha = alpha;
    }

    private static string ShortLabel(LionAction action)
    {
        if (action.ActionKind == LionAction.Kind.Jump) return "JUMP";
        if (action.ActionKind != LionAction.Kind.Step) return string.Empty;

        Vector3 d = action.Direction;
        if (d == Vector3.right) return "\u2191"; // ↑
        if (d == Vector3.left) return "\u2193"; // ↓ 
        if (d == Vector3.forward) return "\u2190"; // ← 
        if (d == Vector3.back) return "\u2192"; // →
        return "?";
    }
}
