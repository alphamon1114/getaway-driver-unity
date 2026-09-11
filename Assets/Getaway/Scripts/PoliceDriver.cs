using UnityEngine;

namespace Getaway
{
    [RequireComponent(typeof(ArcadeCar))]
    public sealed class PoliceDriver : MonoBehaviour
    {
        public ArcadeCar target;
        public WorldBuilder world;
        [Tooltip("Speed in m/s a patrol tries to be down to before threading a barricade gap.")]
        public float threadSpeed = 22;

        ArcadeCar car;
        float stuck;
        float reverseUntil;
        float avoidanceDirection = 1;

        void Awake() { car = GetComponent<ArcadeCar>(); }

        void FixedUpdate()
        {
            if (target == null || !car.drivingEnabled) return;
            Vector3 position = transform.position;
            Vector3 aim = target.transform.position + target.Body.linearVelocity * 0.35f;
            bool threading = false, slowForBlock = false;

            // A barricade line ahead outranks the chase: thread its gap or wear the wall.
            if (world != null && world.NextRoadblock(position.z, out float blockZ, out float gapX))
            {
                float distance = blockZ - position.z;
                // Braking distance under the arcade brake model, plus room to line up with the gap.
                float slowZone = Mathf.Max(12, (car.Speed - threadSpeed) / 4.5f + 14);
                if (distance > 0 && distance < slowZone + 55)
                {
                    // Aiming past the line keeps a forward component as the gap is swallowed.
                    aim = new Vector3(gapX, position.y, blockZ + 8);
                    threading = true;
                    slowForBlock = distance < slowZone && car.Speed > threadSpeed;
                }
            }

            Vector3 local = transform.InverseTransformPoint(aim);
            float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            float steer = Mathf.Clamp(angle / 35, -1, 1);
            // Blind avoidance only helps where there is no better information; the gap aim is better.
            if (!threading)
            {
                Vector3 origin = position + transform.forward * 2.6f;
                if (Physics.SphereCast(origin, 0.75f, transform.forward, out RaycastHit hit, 7 + car.Speed * 0.22f, 1, QueryTriggerInteraction.Ignore))
                {
                    Vector3 obstacle = transform.InverseTransformPoint(hit.collider.bounds.center);
                    avoidanceDirection = obstacle.x >= 0 ? -1 : 1;
                    steer = avoidanceDirection;
                }
            }
            stuck = car.Speed < 1.5f ? stuck + Time.fixedDeltaTime : 0;
            if (stuck > 1.8f) { reverseUntil = Time.time + 1.4f; stuck = 0; }
            bool reverse = Time.time < reverseUntil;
            car.Throttle = reverse ? -1 : slowForBlock ? 0 : 1;
            car.Steering = reverse ? -avoidanceDirection : steer;
            car.Braking = !reverse && (slowForBlock || (Mathf.Abs(angle) > 65 && car.Speed > 9));
        }
    }
}
