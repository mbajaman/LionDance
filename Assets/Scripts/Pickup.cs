using UnityEngine;

/// <summary>
/// Base class for anything the lion can collect. Listens for trigger enter from
/// a tagged collider (the lion's body / either dancer) and forwards the event
/// to subclasses via <see cref="OnPickedUp"/>.
///
/// Requires a Collider with "Is Trigger" enabled on this GameObject.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public abstract class Pickup : MonoBehaviour
{
    [Header("Pickup")]
    [Tooltip("Only colliders with this tag trigger the pickup. Usually 'Player'.")]
    [SerializeField] protected string collectorTag = "Player";

    [Tooltip("If true, the pickup destroys its GameObject after being collected.")]
    [SerializeField] protected bool destroyOnPickup = true;

    [Tooltip("Optional VFX prefab spawned at the pickup's position when collected.")]
    [SerializeField] protected GameObject pickupVfxPrefab;

    [Tooltip("Optional sound played at the pickup's position when collected.")]
    [SerializeField] protected AudioClip pickupSound;

    [SerializeField, Range(0f, 1f)] protected float pickupSoundVolume = 1f;

    private bool _collected;

    protected virtual void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        if (!string.IsNullOrEmpty(collectorTag) && !other.CompareTag(collectorTag)) return;

        var manager = GameManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning($"[{GetType().Name}] No GameManager in scene; pickup on '{name}' ignored.", this);
            return;
        }

        _collected = true;
        OnPickedUp(manager, other);
        PlayFeedback();

        if (destroyOnPickup) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    /// <summary>
    /// Override to apply the pickup's effect (add money, lettuce, etc.).
    /// </summary>
    protected abstract void OnPickedUp(GameManager manager, Collider collector);

    private void PlayFeedback()
    {
        if (pickupVfxPrefab != null)
        {
            Instantiate(pickupVfxPrefab, transform.position, transform.rotation);
        }

        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupSoundVolume);
        }
    }
}
