using UnityEngine;

namespace Getaway
{
    [RequireComponent(typeof(ArcadeCar))]
    public sealed class PoliceDriver : MonoBehaviour
    {
        public ArcadeCar target;
        public bool searching;
        ArcadeCar car;
        float stuck;
        float reverseUntil;
        float avoidanceDirection = 1;

        void Awake() { car = GetComponent<ArcadeCar>(); }

        void FixedUpdate()
        {
            if (target == null || !car.drivingEnabled) return;
            if (searching) { car.Throttle = 0; car.Steering = 0; car.Braking = true; return; }
            Vector3 aim = target.transform.position + target.Body.linearVelocity * 0.35f;
            Vector3 local = transform.InverseTransformPoint(aim);
            float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            float steer = Mathf.Clamp(angle / 35, -1, 1);
            Vector3 origin = transform.position + transform.forward * 2.6f;
            if (Physics.SphereCast(origin, 0.75f, transform.forward, out RaycastHit hit, 7 + car.Speed * 0.22f, 1, QueryTriggerInteraction.Ignore))
            {
                Vector3 obstacle = transform.InverseTransformPoint(hit.collider.bounds.center);
                avoidanceDirection = obstacle.x >= 0 ? -1 : 1;
                steer = avoidanceDirection;
            }
            stuck = car.Speed < 1.5f ? stuck + Time.fixedDeltaTime : 0;
            if (stuck > 1.8f) { reverseUntil = Time.time + 1.4f; stuck = 0; }
            bool reverse = Time.time < reverseUntil;
            car.Throttle = reverse ? -1 : 1;
            car.Steering = reverse ? -avoidanceDirection : steer;
            car.Braking = !reverse && Mathf.Abs(angle) > 65 && car.Speed > 9;
        }
    }
}
