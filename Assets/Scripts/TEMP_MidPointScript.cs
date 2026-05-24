using UnityEngine;

public class MidpointFollower : MonoBehaviour
{
    [SerializeField] private Transform front;
    [SerializeField] private Transform back;

    private void LateUpdate()
    {
        if (front == null || back == null) return;
        transform.position = (front.position + back.position) * 0.5f;
    }
}