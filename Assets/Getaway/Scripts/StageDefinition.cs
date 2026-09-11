using UnityEngine;

namespace Getaway
{
    [CreateAssetMenu(menuName = "Getaway/Stage")]
    public sealed class StageDefinition : ScriptableObject
    {
        public string title = "The First Job";
        [TextArea] public string briefing = "Collect your crew and reach the safehouse.";
        [Min(200)] public float roadLength = 650;
        [Min(30)] public float timeLimit = 100;
        [Range(1, 6)] public int policeCount = 2;
        [Range(12, 35)] public float policeSpeed = 25;
        [Min(20)] public float escapeDistance = 65;
        [Min(1)] public float escapeSeconds = 5;
        [Range(1, 6)] public int crewCount = 3;
        public int seed = 17;
        [Header("Bank appointment")]
        [Min(40)] public float bankDistance = 180;
        [Min(1)] public float crewExitTime = 14;
        [Min(3)] public float lateGrace = 12;
        [Min(1)] public float arrivalScoreWindow = 10;
        [Min(0)] public int maxArrivalScore = 1000;
        [Header("Stolen money")]
        [Min(0)] public int startingLoot = 12000;
        [Min(0)] public float cashLossPerDamage = 120;
    }
}
