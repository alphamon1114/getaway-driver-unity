using System;
using UnityEngine;

namespace Getaway
{
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class ArcadeCar : MonoBehaviour
    {
        public bool playerControlled;
        public bool drivingEnabled = true;
        [Tooltip("Only cars that drift respond to the handbrake. Police keep full grip so they stay on the player.")]
        public bool handbrakeDrift = true;

        [Header("Performance")]
        public float topSpeed = 36;
        public float acceleration = 14;
        public float reverseTopSpeed = 11;

        [Header("Steering")]
        [Tooltip("Yaw rate in degrees per second at walking pace.")]
        public float turnSpeed = 120;
        [Tooltip("Yaw rate in degrees per second at top speed. Keeping this well below turnSpeed stops the car feeling twitchy on the straights.")]
        public float highSpeedTurnSpeed = 52;
        [Tooltip("How fast the wheels follow the key press, in steering units per second. Lower feels heavier.")]
        public float steerResponse = 8;
        [Tooltip("Below this speed in m/s the car stops responding to steering, so it cannot pirouette while parked.")]
        public float steerFadeSpeed = 2.5f;

        [Header("Grip and drift")]
        public float lateralGrip = 9;
        public float driftGrip = 2.2f;
        [Tooltip("How fast grip returns after the handbrake is released. Low values let the slide run on for a moment.")]
        public float gripRecovery = 5;
        public float driftYawBonus = 1.4f;

        [Header("Drag")]
        public float throttleDrag = 0.08f;
        public float coastDrag = 0.16f;
        public float brakeDrag = 4.5f;
        [Tooltip("Longitudinal drag during a steered handbrake slide, so a drift carries speed through the corner.")]
        public float driftBrakeDrag = 0.8f;

        public float health = 100;
        public float maxHealth = 100;
        public float collisionDamageMultiplier = 1;
        public event Action<float, string> Damaged;
        public event Action<float> PoliceImpact;

        public Rigidbody Body { get; private set; }
        public float Speed => Body == null ? 0 : Body.linearVelocity.magnitude;
        public float Kph => Speed * 3.6f;
        public float Throttle { get; set; }
        public float Steering { get; set; }
        public bool Braking { get; set; }
        /// <summary>Signed degrees between where the car points and where it is actually travelling.</summary>
        public float SlipAngle { get; private set; }
        public bool Drifting { get; private set; }
        public bool Grounded { get; private set; }

        float steerNow;
        float grip;
        float lastDamage = -10;

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Body.mass = 1200;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            Body.centerOfMass = new Vector3(0, -0.3f, 0);
            grip = lateralGrip;
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
            float dt = Time.fixedDeltaTime;
            if (!drivingEnabled || health <= 0)
            {
                Body.linearVelocity = Vector3.Lerp(Body.linearVelocity, Vector3.zero, 10 * dt);
                Body.angularVelocity = Vector3.zero;
                steerNow = 0; SlipAngle = 0; Drifting = false;
                return;
            }

            // World geometry is on layer 0; vehicles are on layer 8, so a car never detects itself.
            Grounded = Physics.Raycast(transform.position, Vector3.down, 1.15f, 1, QueryTriggerInteraction.Ignore);

            // Smoothing the digital key press is what turns on/off input into a weighted car.
            steerNow = Mathf.MoveTowards(steerNow, Mathf.Clamp(Steering, -1, 1), steerResponse * dt);

            // Zeroed even in the air so a clipped kerb cannot spin the car and land it sideways.
            Body.angularVelocity = Vector3.zero;
            if (!Grounded) { SlipAngle = 0; Drifting = false; return; }

            Vector3 local = transform.InverseTransformDirection(Body.linearVelocity);
            float forward = local.z;
            float speed = Mathf.Abs(forward);
            float throttle = Mathf.Clamp(Throttle, -1, 1);

            SlipAngle = local.magnitude < 1.5f ? 0 : Mathf.Atan2(local.x, Mathf.Abs(local.z)) * Mathf.Rad2Deg;
            Drifting = handbrakeDrift && Braking && speed > 8 && Mathf.Abs(steerNow) > 0.25f;

            // Drive. Pushing against the direction of travel bites harder, and the last fifth of the
            // speed range tapers off instead of hitting a wall at exactly topSpeed.
            float accel = throttle * acceleration;
            if (throttle > 0)
                accel *= forward < -0.5f ? 2.2f : Mathf.Clamp01((topSpeed - forward) / (topSpeed * 0.2f));
            else if (throttle < 0)
                accel *= forward > 0.5f ? 2.2f : Mathf.Clamp01((reverseTopSpeed + forward) / (reverseTopSpeed * 0.35f));
            Body.AddForce(transform.forward * accel, ForceMode.Acceleration);

            // Grip collapses the instant the handbrake bites and returns slowly, so the slide runs on.
            float targetGrip = Drifting ? driftGrip : lateralGrip;
            grip = Mathf.MoveTowards(grip, targetGrip, (Drifting ? 40 : gripRecovery) * dt);
            Body.AddForce(-transform.right * local.x * grip, ForceMode.Acceleration);

            // Braking straight stops the car hard; braking into a turn trades stopping power for a slide.
            float drag;
            if (Braking)
            {
                float slide = handbrakeDrift ? Mathf.Abs(steerNow) * Mathf.Clamp01(speed / 9f) : 0;
                drag = Mathf.Lerp(brakeDrag, driftBrakeDrag, slide);
            }
            else drag = Mathf.Abs(throttle) > 0.01f ? throttleDrag : coastDrag;
            Body.AddForce(-transform.forward * forward * drag, ForceMode.Acceleration);

            // Agile at parking speed, calm at motorway speed. A flat yaw rate is what makes an
            // arcade car feel like a tank up top and like a brick down low.
            float rate = Mathf.Lerp(turnSpeed, highSpeedTurnSpeed, Mathf.Clamp01(speed / topSpeed));
            if (Drifting) rate *= driftYawBonus;
            float fade = Mathf.Clamp01(speed / steerFadeSpeed);
            float direction = forward < -0.2f ? -1 : 1;
            Body.MoveRotation(Body.rotation * Quaternion.Euler(0, steerNow * rate * fade * direction * dt, 0));
            Body.angularVelocity = Vector3.zero;
        }

        void OnCollisionEnter(Collision hit)
        {
            if (!drivingEnabled || Time.time - lastDamage < 0.6f) return;
            float impact = hit.relativeVelocity.magnitude;
            if (impact < 4) return;
            lastDamage = Time.time;
            bool police = hit.rigidbody != null && hit.rigidbody.GetComponent<PoliceDriver>() != null;
            ApplyDamage(Mathf.Clamp((impact - 3) * 1.4f, 0, 35) * collisionDamageMultiplier, hit.gameObject.name, police);
        }

        public void ApplyDamage(float amount, string source, bool policeCollision = false)
        {
            if (health <= 0 || amount <= 0) return;
            amount = Mathf.Min(health, amount);
            health = Mathf.Max(0, health - amount);
            Damaged?.Invoke(amount, source);
            if (policeCollision) PoliceImpact?.Invoke(amount);
        }

        public void ApplyLoadout(OwnedVehicle owned)
        {
            var spec = GarageCatalog.Find(owned.id);
            topSpeed = spec.Speed * (1 + owned.engine * 0.08f);
            acceleration = spec.Acceleration * (1 + owned.engine * 0.10f);
            health = maxHealth = spec.Health;
            lateralGrip = spec.Grip * (1 + owned.tires * 0.10f);
            gripRecovery = 5 + owned.tires;
            collisionDamageMultiplier = 1 - owned.armor * 0.12f;
            grip = lateralGrip;
        }

        public void Recover(Vector3 position)
        {
            // Toggling interpolation clears the interpolation history, otherwise the car smears
            // across the map from wherever it was when R was pressed.
            RigidbodyInterpolation previous = Body.interpolation;
            Body.interpolation = RigidbodyInterpolation.None;
            Body.position = position;
            Body.rotation = Quaternion.identity;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
            Body.interpolation = previous;
            steerNow = 0; grip = lateralGrip; SlipAngle = 0; Drifting = false;
            ApplyDamage(8, "vehicle_recovery");
        }
    }
}
