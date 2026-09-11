using UnityEngine;

namespace Getaway
{
    public sealed class FollowCamera : MonoBehaviour
    {
        public Transform target;
        Vector3 velocity;
        void LateUpdate()
        {
            if (target == null) return;
            Vector3 focus = target.position + Vector3.up * 1.4f;
            Vector3 desired = target.position - target.forward * 10 + Vector3.up * 6;
            Vector3 ray = desired - focus;
            if (Physics.SphereCast(focus, 0.35f, ray.normalized, out RaycastHit hit, ray.magnitude, 1, QueryTriggerInteraction.Ignore))
                desired = focus + ray.normalized * Mathf.Max(1, hit.distance - 0.25f);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, 0.12f, Mathf.Infinity, Time.unscaledDeltaTime);
            transform.LookAt(focus + target.forward * 4);
        }
    }
}
