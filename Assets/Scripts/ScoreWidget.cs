using Rive;
using Rive.Components;
using UnityEngine;

/// <summary>
/// Drives a Rive string property from the <see cref="GameManager"/> score.
/// Attach this to the same GameObject as the RiveWidget and set the Rive
/// property name to the string field in the bound ViewModel.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RiveWidget))]
public class ScoreWidget : MonoBehaviour
{
    public enum TrackedValue
    {
        Money,
        Lettuce,
    }

    [Header("Rive")]
    [Tooltip("The Rive ViewModel string property to update.")]
    [SerializeField] private string scorePropertyName = "ScoreCount";

    [Tooltip("Optional ViewModel name used only if the widget has not already auto-bound a ViewModel instance.")]
    [SerializeField] private string fallbackViewModelName = "ViewModel1";

    [Header("Score")]
    [Tooltip("Which GameManager value to display.")]
    [SerializeField] private TrackedValue tracks = TrackedValue.Money;

    private RiveWidget _widget;
    private ViewModelInstanceStringProperty _scoreProperty;
    private GameManager _subscribed;
    private bool _riveBindingAttempted;

    private void Awake()
    {
        _widget = GetComponent<RiveWidget>();
    }

    private void OnEnable()
    {
        if (_widget != null)
        {
            _widget.OnWidgetStatusChanged += OnWidgetStatusChanged;
        }

        TrySubscribe();
        TryBindScoreProperty();
        Refresh();
    }

    private void OnDisable()
    {
        if (_widget != null)
        {
            _widget.OnWidgetStatusChanged -= OnWidgetStatusChanged;
        }

        Unsubscribe();
        _scoreProperty = null;
        _riveBindingAttempted = false;
    }

    private void Update()
    {
        if (_subscribed == null) TrySubscribe();
        if (_scoreProperty == null && !_riveBindingAttempted) TryBindScoreProperty();
    }

    private void OnWidgetStatusChanged()
    {
        _scoreProperty = null;
        _riveBindingAttempted = false;
        TryBindScoreProperty();
        Refresh();
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

    private void OnMoneyChanged(int previous, int current) => SetScore(current);
    private void OnLettuceChanged(int previous, int current) => SetScore(current);

    private void Refresh()
    {
        if (_subscribed == null)
        {
            SetScore(0);
            return;
        }

        SetScore(tracks == TrackedValue.Money ? _subscribed.Money : _subscribed.LettuceCount);
    }

    private void TryBindScoreProperty()
    {
        if (_widget == null || string.IsNullOrWhiteSpace(scorePropertyName)) return;
        if (_widget.Status != WidgetStatus.Loaded || _widget.StateMachine == null) return;

        _riveBindingAttempted = true;
        ViewModelInstance viewModelInstance = _widget.StateMachine.ViewModelInstance;

        if (viewModelInstance == null)
        {
            viewModelInstance = CreateFallbackViewModelInstance();
            if (viewModelInstance != null)
            {
                _widget.StateMachine.BindViewModelInstance(viewModelInstance);
            }
        }

        _scoreProperty = viewModelInstance?.GetStringProperty(scorePropertyName);
        if (_scoreProperty != null) Refresh();
    }

    private ViewModelInstance CreateFallbackViewModelInstance()
    {
        ViewModel viewModel = null;

        if (!string.IsNullOrWhiteSpace(fallbackViewModelName))
        {
            viewModel = _widget.File?.GetViewModelByName(fallbackViewModelName);
        }

        viewModel ??= _widget.Artboard?.DefaultViewModel;
        return viewModel?.CreateDefaultInstance();
    }

    private void SetScore(int value)
    {
        if (_scoreProperty == null) return;
        _scoreProperty.Value = value.ToString();
    }
}
