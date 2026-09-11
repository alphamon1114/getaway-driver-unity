using System;
using UnityEngine;

namespace Getaway
{
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class ArcadeCar : MonoBehaviour
    {
        public bool playerControlled;
        public bool drivingEnabled = true;
        public float topSpeed = 36;
        public float acceleration = 14;
        public float turnSpeed = 75;
        public float health = 100;
        public event Action<float, string> Damaged;
        public Rigidbody Body { get; private set; }
        public float Speed => Body == null ? 0 : Body.linearVelocity.magnitude;
        public float Kph => Speed * 3.6f;
        public float Throttle { get; set; }
        public float Steering { get; set; }
        public bool Braking { get; set; }
        float lastDamage = -10;

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Body.mass = 1200;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            Body.centerOfMass = new Vector3(0, -0.3f, 0);
        }

        void Update()
        {
            if (!playerControlled) return;
            Throttle = GameInput.Throttle;
            Steering = GameInput.Steer;
            Braking = GameInput.Brake;
        }

        void FixedUpdate()
        {
            if (!drivingEnabled || health <= 0)
            {
                Body.linearVelocity = Vector3.Lerp(Body.linearVelocity, Vector3.zero, 10 * Time.fixedDeltaTime);
                Body.angularVelocity = Vector3.zero;
                return;
            }
            // World geometry is on layer 0; vehicles are on layer 8.
            bool grounded = Physics.Raycast(transform.position, Vector3.down, 1.15f, 1, QueryTriggerInteraction.Ignore);
            if (!grounded) return;
            Vector3 local = transform.InverseTransformDirection(Body.linearVelocity);
            float throttle = Mathf.Clamp(Throttle, -1, 1);
            float accel = throttle * acceleration;
            if (local.z * throttle < -0.5f) accel *= 2.2f;
            if (local.z >= topSpeed && accel > 0 || local.z <= -10 && accel < 0) accel = 0;
            Body.AddForce(transform.forward * accel, ForceMode.Acceleration);
            Body.AddForce(-transform.right * local.x * (Braking ? 2 : 8), ForceMode.Acceleration);
            Body.AddForce(-transform.forward * local.z * (Braking ? 3.8f : 0.12f), ForceMode.Acceleration);
            float steeringWeight = Mathf.Clamp01(Mathf.Abs(local.z) / 5);
            float direction = local.z < -0.2f ? -1 : 1;
            Body.MoveRotation(Body.rotation * Quaternion.Euler(0, Mathf.Clamp(Steering, -1, 1) * turnSpeed * steeringWeight * direction * Time.fixedDeltaTime, 0));
            Body.angularVelocity = Vector3.zero;
        }

        void OnCollisionEnter(Collision hit)
        {
            if (!drivingEnabled || Time.time - lastDamage < 0.6f) return;
            float impact = hit.relativeVelocity.magnitude;
            if (impact < 4) return;
            lastDamage = Time.time;
            ApplyDamage(Mathf.Clamp((impact - 3) * 1.4f, 0, 35), hit.gameObject.name);
        }

        public void ApplyDamage(float amount, string source)
        {
            if (health <= 0 || amount <= 0) return;
            health = Mathf.Max(0, health - amount);
            Damaged?.Invoke(amount, source);
        }

        public void Recover(Vector3 position)
        {
            Body.position = position;
            Body.rotation = Quaternion.identity;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            ApplyDamage(8, "vehicle_recovery");
        }
    }
}
