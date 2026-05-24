using UnityEngine;

/// <summary>
/// Lettuce (cai qing). Adds to the lettuce-eaten counter and optionally awards
/// bonus money, mirroring the traditional reward inside the lettuce bundle.
/// </summary>
public class LettucePickup : Pickup
{
    [Header("Lettuce")]
    [Tooltip("Lettuce heads added to the GameManager when this pickup is collected.")]
    [SerializeField, Min(1)] private int lettuceValue = 1;

    [Tooltip("Bonus money awarded on top of the lettuce count. Set to 0 for none.")]
    [SerializeField, Min(0)] private int bonusMoney = 0;

    protected override void OnPickedUp(GameManager manager, Collider collector)
    {
        manager.AddLettuce(lettuceValue);
        if (bonusMoney > 0) manager.AddMoney(bonusMoney);
    }
}
