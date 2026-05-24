using TMPro;
using UnityEngine;

/// <summary>
/// Drives a TextMeshPro label from the <see cref="GameManager"/> score.
/// Drop on a Canvas child, drag the TMP_Text into <see cref="label"/>, pick
/// which value to show, and it stays in sync automatically.
/// </summary>
[DisallowMultipleComponent]
public class ScoreHUD : MonoBehaviour
{
    public enum TrackedValue
    {
        Money,
        Lettuce,
    }

    [Header("Target")]
    [Tooltip("The TMP label this HUD writes to. Works with both TextMeshProUGUI (Canvas) and TextMeshPro (world space).")]
    [SerializeField] private TMP_Text label;

    [Tooltip("Which GameManager value to display.")]
    [SerializeField] private TrackedValue tracks = TrackedValue.Money;

    [Header("Formatting")]
    [Tooltip("Format string. {0} is the value. Examples: '${0}', 'Score: {0:N0}', 'Lettuce x{0}'.")]
    [SerializeField] private string format = "${0}";

    private GameManager _subscribed;

    private void Reset()
    {
        if (label == null) label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        TrySubscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (_subscribed == null) TrySubscribe();
    }

    private void TrySubscribe()
    {
        var manager = GameManager.Instance;
        if (manager == null || manager == _subscribed) return;

        Unsubscribe();
        _subscribed = manager;

        if (tracks == TrackedValue.Money) _subscribed.MoneyChanged += OnMoneyChanged;
        else _subscribed.LettuceChanged += OnLettuceChanged;

        Refresh();
    }

    private void Unsubscribe()
    {
        if (_subscribed == null) return;

        _subscribed.MoneyChanged -= OnMoneyChanged;
        _subscribed.LettuceChanged -= OnLettuceChanged;
        _subscribed = null;
    }

    private void OnMoneyChanged(int previous, int current) => SetText(current);
    private void OnLettuceChanged(int previous, int current) => SetText(current);

    private void Refresh()
    {
        if (_subscribed == null)
        {
            SetText(0);
            return;
        }

        SetText(tracks == TrackedValue.Money ? _subscribed.Money : _subscribed.LettuceCount);
    }

    private void SetText(int value)
    {
        if (label == null) return;
        label.text = string.IsNullOrEmpty(format) ? value.ToString() : string.Format(format, value);
    }
}
