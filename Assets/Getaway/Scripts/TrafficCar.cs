using UnityEngine;

namespace Getaway
{
    /// <summary>
    /// Civilian traffic crossing the main road. It drives one axis at a steady speed and wraps
    /// round at the end of its street, so an intersection is a timing problem rather than scenery.
    /// Not a <see cref="PoliceDriver"/>, so hitting one costs hull but never cash.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class TrafficCar : MonoBehaviour
    {
        [Tooltip("+1 drives toward +x, -1 toward -x.")]
        public int heading = 1;
        public float speed = 11;
        public float minX = -65, maxX = 65;
        [Tooltip("Centre of the lane this car holds, in z.")]
        public float laneZ;

        Rigidbody body;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = 1100;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.centerOfMass = new Vector3(0, -0.3f, 0);
            if (heading == 0) heading = 1;
        }

        void FixedUpdate()
        {
            Vector3 position = body.position;
            if (heading > 0 && position.x > maxX) Wrap(minX);
            else if (heading < 0 && position.x < minX) Wrap(maxX);

            // Steady cruise along the street, with a gentle pull back into the lane after a shunt.
            Vector3 velocity = body.linearVelocity;
            Vector3 wanted = new Vector3(heading * speed, velocity.y, (laneZ - body.position.z) * 1.5f);
            body.AddForce((wanted - velocity) * 4, ForceMode.Acceleration);
            body.MoveRotation(Quaternion.Euler(0, heading > 0 ? 90 : -90, 0));
            body.angularVelocity = Vector3.zero;
        }

        void Wrap(float x)
        {
            // Interpolation has to be cycled or the car smears the width of the street.
            RigidbodyInterpolation previous = body.interpolation;
            body.interpolation = RigidbodyInterpolation.None;
            Vector3 target = new Vector3(x, body.position.y, laneZ);
            body.position = target;
            transform.position = target;
            body.linearVelocity = new Vector3(heading * speed, 0, 0);
            body.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
            body.interpolation = previous;
        }
    }
}
