using UnityEngine;

namespace Getaway
{
    [RequireComponent(typeof(Camera))]
    public sealed class FollowCamera : MonoBehaviour
    {
        public Transform target;

        [Header("Framing")]
        [Tooltip("Distance behind the car at a standstill and at reference speed.")]
        public float nearDistance = 8.5f;
        public float farDistance = 12.5f;
        public float nearHeight = 4.6f;
        public float farHeight = 5.8f;

        [Header("Speed feel")]
        [Tooltip("Degrees of field of view added on top of the camera's authored value at reference speed.")]
        public float speedFov = 14;
        [Tooltip("Speed in m/s treated as flat out for framing purposes.")]
        public float referenceSpeed = 34;
        [Tooltip("How far the camera swings to the outside of a slide, in metres.")]
        public float driftOffset = 2.6f;

        Camera cam;
        ArcadeCar car;
        Transform cached;
        Vector3 velocity;
        float baseFov = -1;
        float speedT, drift;

        void Awake() { cam = GetComponent<Camera>(); }

        void LateUpdate()
        {
            if (target == null) return;
            if (cam == null) cam = GetComponent<Camera>();
            // Captured lazily so the camera keeps whatever field of view the scene authored.
            if (baseFov < 0 && cam != null) baseFov = cam.fieldOfView;
            if (cached != target) { cached = target; car = target.GetComponentInParent<ArcadeCar>(); }

            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            float speed = car != null ? car.Speed : 0;
            // Easing these rather than reading them raw keeps the frame from pumping on every bump.
            speedT = Mathf.MoveTowards(speedT, Mathf.Clamp01(speed / Mathf.Max(1, referenceSpeed)), 1.4f * dt);
            float slip = car != null ? Mathf.Clamp(car.SlipAngle / 35f, -1, 1) : 0;
            drift = Mathf.MoveTowards(drift, slip, 2.5f * dt);

            Vector3 focus = target.position + Vector3.up * 1.4f;
            Vector3 desired = target.position
                - target.forward * Mathf.Lerp(nearDistance, farDistance, speedT)
                + Vector3.up * Mathf.Lerp(nearHeight, farHeight, speedT)
                + target.right * drift * driftOffset;

            Vector3 ray = desired - focus;
            if (Physics.SphereCast(focus, 0.35f, ray.normalized, out RaycastHit hit, ray.magnitude, 1, QueryTriggerInteraction.Ignore))
                desired = focus + ray.normalized * Mathf.Max(1, hit.distance - 0.25f);

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, Mathf.Lerp(0.13f, 0.09f, speedT), Mathf.Infinity, dt);
            transform.LookAt(focus + target.forward * 4 + target.right * drift * 1.5f);
            if (cam != null && baseFov > 0)
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, baseFov + speedFov * speedT, 6 * dt);
        }
    }
}
