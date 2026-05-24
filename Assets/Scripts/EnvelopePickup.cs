using UnityEngine;

/// <summary>
/// Red envelope (hong bao). Awards money to the player on pickup.
/// </summary>
public class EnvelopePickup : Pickup
{
    [Header("Envelope")]
    [Tooltip("Money added to the GameManager when this envelope is collected.")]
    [SerializeField, Min(0)] private int moneyValue = 100;

    protected override void OnPickedUp(GameManager manager, Collider collector)
    {
        manager.AddMoney(moneyValue);
    }
}
