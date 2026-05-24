using System;
using UnityEngine;

/// <summary>
/// Central scorekeeper for the lion dance run. Tracks money (from red envelopes)
/// and lettuce eaten (from cai qing pickups). Exposes events so UI or audio can
/// react without polling.
/// </summary>
[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Starting Values")]
    [SerializeField, Min(0)] private int startingMoney = 0;
    [SerializeField, Min(0)] private int startingLettuce = 0;

    [Header("Behaviour")]
    [Tooltip("If true, this manager persists across scene loads. Leave off if each scene has its own.")]
    [SerializeField] private bool dontDestroyOnLoad = false;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;

    public int Money { get; private set; }
    public int LettuceCount { get; private set; }

    public event Action<int, int> MoneyChanged;
    public event Action<int, int> LettuceChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[GameManager] Duplicate instance on '{name}' destroyed; using '{Instance.name}'.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        Money = startingMoney;
        LettuceCount = startingLettuce;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddMoney(int amount)
    {
        if (amount == 0) return;

        int previous = Money;
        Money = Mathf.Max(0, Money + amount);

        if (verboseLogging)
        {
            Debug.Log($"[GameManager] Money {previous} -> {Money} ({(amount >= 0 ? "+" : "")}{amount}).");
        }

        MoneyChanged?.Invoke(previous, Money);
    }

    public void AddLettuce(int amount)
    {
        if (amount == 0) return;

        int previous = LettuceCount;
        LettuceCount = Mathf.Max(0, LettuceCount + amount);

        if (verboseLogging)
        {
            Debug.Log($"[GameManager] Lettuce {previous} -> {LettuceCount} ({(amount >= 0 ? "+" : "")}{amount}).");
        }

        LettuceChanged?.Invoke(previous, LettuceCount);
    }

    public void ResetScore()
    {
        AddMoney(startingMoney - Money);
        AddLettuce(startingLettuce - LettuceCount);
    }
}
