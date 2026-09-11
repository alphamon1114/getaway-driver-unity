using UnityEngine;

namespace Getaway
{
    [CreateAssetMenu(menuName = "Getaway/Stage")]
    public sealed class StageDefinition : ScriptableObject
    {
        public string title = "The First Job";
        [TextArea] public string briefing = "Beat the roadblocks to the bank, then run the crew out of the city.";
        [Min(200)] public float roadLength = 650;
        [Min(30)] public float timeLimit = 100;
        [Range(1, 6)] public int crewCount = 3;
        public int seed = 17;

        [Header("Police")]
        [Range(1, 6)] public int policeCount = 2;
        [Tooltip("Straight-line top speed in m/s. Set this slightly above the fastest fully upgraded car so patrols stay a threat.")]
        [Range(12, 80)] public float policeSpeed = 38;
        [Tooltip("Patrol acceleration in m/s^2. Keep it below the player's so they lose ground out of every roadblock.")]
        [Range(4, 20)] public float policeAcceleration = 9;

        [Header("Bank deadline")]
        [Min(40)] public float bankDistance = 180;
        [Tooltip("Seconds allowed to finish boarding at the bank. Missing it fails the job.")]
        [Min(2)] public float bankDeadline = 14;
        [Tooltip("Arriving at or before this time scores full marks. Score falls to zero at the deadline.")]
        [Min(1)] public float arrivalParTime = 7;
        [Min(0)] public int maxArrivalScore = 1000;

        [Header("Roadblocks")]
        [Min(20)] public float firstRoadblock = 55;
        [Min(25)] public float roadblockSpacing = 55;
        [Tooltip("Width in metres of the one gap left open in each barricade line.")]
        [Range(5, 16)] public float roadblockGap = 7;

        [Header("Escape route")]
        [Tooltip("Distance back from the end of the main road where the highway out of the city branches off.")]
        [Min(40)] public float exitJunctionOffset = 120;
        [Tooltip("How far the escape road runs out of the city, measured from the main road's edge.")]
        [Min(60)] public float exitRoadLength = 170;

        [Header("Stolen money")]
        [Min(0)] public int startingLoot = 12000;
        [Min(0)] public float cashLossPerDamage = 120;
    }
}
