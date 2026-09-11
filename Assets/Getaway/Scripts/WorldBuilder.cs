using System.Collections.Generic;
using UnityEngine;

namespace Getaway
{
    public sealed class WorldBuilder : MonoBehaviour
    {
        public GameObject playerVisualPrefab;
        public GameObject coupeVisualPrefab;
        public GameObject vanVisualPrefab;
        public GameObject policeVisualPrefab;
        public GameObject crewVisualPrefab;

        // Half width of the drivable surface. Boundary walls sit one metre outside it.
        public const float RoadHalfWidth = 16;
        // Half width in z of the mouth where the escape road leaves the city.
        public const float JunctionHalfWidth = 16;

        public Vector3 Pickup { get; private set; }
        public Vector3 Destination { get; private set; }
        /// <summary>Centre of the junction where the escape road branches off the main road.</summary>
        public float JunctionZ { get; private set; }
        public ArcadeCar Player { get; private set; }
        public readonly List<PoliceDriver> Police = new List<PoliceDriver>();
        // x is the barricade line's z position, y is the x centre of its open gap.
        readonly List<Vector2> roadblocks = new List<Vector2>();
        readonly List<GameObject> crew = new List<GameObject>();
        readonly List<Material> materials = new List<Material>();
        GameObject root;
        PhysicsMaterial slippery;

        /// <summary>First barricade line ahead of z, so drivers can aim at its gap instead of the wall.</summary>
        public bool NextRoadblock(float z, out float blockZ, out float gapX)
        {
            blockZ = gapX = 0;
            for (int i = 0; i < roadblocks.Count; i++)
                if (roadblocks[i].x > z) { blockZ = roadblocks[i].x; gapX = roadblocks[i].y; return true; }
            return false;
        }

        Material Material(Color color)
        {
            // Resources keeps the shader in standalone builds despite procedural geometry.
            var shader = Resources.Load<Shader>("GetawayLit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = color;
            mat.SetFloat("_Glossiness", 0.2f);
            materials.Add(mat);
            return mat;
        }
        GameObject Box(string label, Transform parent, Vector3 position, Vector3 scale, Material mat, bool solid = true)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = label;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = mat;
            if (!solid) { obj.GetComponent<Collider>().enabled = false; Destroy(obj.GetComponent<Collider>()); }
            return obj;
        }
        // Spans are clipped to nothing when an opening swallows them, which is how the boundary
        // wall and each barricade line get their gaps without special cases at the call site.
        void ZSpan(string label, float x, float y, float from, float to, float width, float height, Material mat, bool solid = true)
        {
            if (to - from <= 0.2f) return;
            Box(label, root.transform, new Vector3(x, y, (from + to) / 2), new Vector3(width, height, to - from), mat, solid);
        }
        void XSpan(string label, float z, float y, float from, float to, float depth, float height, Material mat, bool solid = true)
        {
            if (to - from <= 0.2f) return;
            Box(label, root.transform, new Vector3((from + to) / 2, y, z), new Vector3(to - from, height, depth), mat, solid);
        }

        public void Build(StageDefinition stage, OwnedVehicle loadout = null)
        {
            Clear();
            root = new GameObject("Generated Stage");
            var road = Material(new Color(0.10f, 0.13f, 0.17f));
            var ground = Material(new Color(0.20f, 0.24f, 0.25f));
            var line = Material(new Color(0.9f, 0.74f, 0.32f));
            var building = Material(new Color(0.28f, 0.35f, 0.42f));
            var windows = Material(new Color(0.69f, 0.85f, 0.88f));
            var orange = Material(new Color(0.93f, 0.30f, 0.09f));
            var green = Material(new Color(0.17f, 0.78f, 0.52f));
            var blue = Material(new Color(0.10f, 0.55f, 0.9f));
            var barrier = Material(new Color(0.95f, 0.82f, 0.16f));

            float length = stage.roadLength;
            Pickup = new Vector3(0, 0.65f, Mathf.Clamp(stage.bankDistance, 40, length - 160));
            JunctionZ = Mathf.Clamp(length - stage.exitJunctionOffset, Pickup.z + 90, length - 40);
            float exitEndX = RoadHalfWidth + stage.exitRoadLength;
            Destination = new Vector3(exitEndX - 22, 0.65f, JunctionZ);
            slippery = new PhysicsMaterial("Arcade car surface") { dynamicFriction = 0, staticFriction = 0, frictionCombine = PhysicsMaterialCombine.Minimum, bounciness = 0 };

            Box("Ground", root.transform, new Vector3(0, -0.6f, length / 2), new Vector3(160, 1, length + 160), ground);
            Box("Road", root.transform, new Vector3(0, -0.1f, length / 2), new Vector3(RoadHalfWidth * 2, 0.2f, length + 100), road);

            // Escape road: the highway out of the city, leaving the right-hand side near the top of the map.
            float mouth0 = JunctionZ - JunctionHalfWidth, mouth1 = JunctionZ + JunctionHalfWidth;
            XSpan("Exit ground", JunctionZ, -0.6f, RoadHalfWidth, exitEndX + 50, JunctionHalfWidth * 2 + 90, 1, ground);
            XSpan("Exit road", JunctionZ, -0.1f, RoadHalfWidth - 2, exitEndX, JunctionHalfWidth * 2, 0.2f, road);
            for (int side = -1; side <= 1; side += 2)
                XSpan("Exit boundary", JunctionZ + side * (JunctionHalfWidth + 1), 1, RoadHalfWidth, exitEndX + 6, 1, 2, building);
            Box("City limits gate", root.transform, new Vector3(exitEndX + 5, 1, JunctionZ), new Vector3(1, 2, JunctionHalfWidth * 2), building);

            // Main road boundaries. The right-hand wall opens only at the junction mouth.
            ZSpan("Road boundary", -17, 1, -50, length + 50, 1, 2, building);
            ZSpan("Road boundary", 17, 1, -50, mouth0, 1, 2, building);
            ZSpan("Road boundary", 17, 1, mouth1, length + 50, 1, 2, building);
            ZSpan("Edge stripe", -15, 0.02f, -45, length + 45, 0.15f, 0.02f, line, false);
            ZSpan("Edge stripe", 15, 0.02f, -45, mouth0, 0.15f, 0.02f, line, false);
            ZSpan("Edge stripe", 15, 0.02f, mouth1, length + 45, 0.15f, 0.02f, line, false);
            Box("End barrier", root.transform, new Vector3(0, 1, length + 45), new Vector3(35, 2, 1), building);
            Box("Start barrier", root.transform, new Vector3(0, 1, -48), new Vector3(35, 2, 1), building);
            for (int z = -30; z < length + 35; z += 12)
                Box("Lane marking", root.transform, new Vector3(0, 0.025f, z), new Vector3(0.2f, 0.03f, 5), line, false);
            for (float x = RoadHalfWidth + 10; x < exitEndX - 6; x += 12)
                Box("Exit lane marking", root.transform, new Vector3(x, 0.025f, JunctionZ), new Vector3(5, 0.03f, 0.2f), line, false);

            var rng = new System.Random(stage.seed);
            for (int z = 0; z < length; z += 28)
                for (int side = -1; side <= 1; side += 2)
                {
                    // Leave the junction mouth clear so the escape road is visible from the main road.
                    if (side > 0 && Mathf.Abs(z - JunctionZ) < JunctionHalfWidth + 14) continue;
                    float h = 9 + rng.Next(20);
                    Box("City block", root.transform, new Vector3(side * 30, h / 2, z), new Vector3(18, h, 21), building);
                    for (int y = 3; y < h - 1; y += 4)
                        Box("Window strip", root.transform, new Vector3(side * 20.9f, y, z), new Vector3(0.1f, 1.4f, 15), windows, false);
                }

            BuildRoadblocks(stage, rng, barrier, blue);

            Box("Pickup zone", root.transform, new Vector3(0, 0.04f, Pickup.z), new Vector3(12, 0.06f, 12), blue, false);
            Box("Bank", root.transform, new Vector3(25, 5, Pickup.z), new Vector3(14, 10, 20), blue);
            Box("Escape zone", root.transform, new Vector3(Destination.x, 0.04f, JunctionZ), new Vector3(20, 0.06f, JunctionHalfWidth * 2), green, false);
            Label("BANK / PICKUP", new Vector3(0, 6, Pickup.z), blue.color);
            Label("CITY LIMITS", new Vector3(Destination.x, 7, JunctionZ), green.color);
            Label("EXIT >>", new Vector3(9, 6, JunctionZ), green.color);

            for (int i = 0; i < stage.crewCount; i++)
            {
                GameObject member;
                if (crewVisualPrefab != null) member = Instantiate(crewVisualPrefab, root.transform);
                else
                {
                    member = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    member.transform.SetParent(root.transform);
                    member.GetComponent<Renderer>().sharedMaterial = orange;
                }
                DisablePhysics(member);
                member.name = "Crew " + (i + 1);
                member.transform.position = new Vector3(5.5f, 1, Pickup.z - 3 + i * 2);
                crew.Add(member);
            }

            if (loadout == null) loadout = new OwnedVehicle { id = "sedan" };
            Color paint = loadout.id == "coupe" ? new Color(0.8f, 0.12f, 0.20f) : loadout.id == "van" ? new Color(0.18f, 0.55f, 0.4f) : new Color(0.9f, 0.48f, 0.13f);
            GameObject prefab = loadout.id == "coupe" ? coupeVisualPrefab : loadout.id == "van" ? vanVisualPrefab : playerVisualPrefab;
            Player = Car("Player", new Vector3(0, 0.8f, 5), Material(paint), prefab);
            Player.playerControlled = true;
            Player.ApplyLoadout(loadout);
            if (prefab == null)
            {
                var cabin = Player.transform.Find("Visual/Cabin");
                if (loadout.id == "van") { cabin.localScale = new Vector3(1.8f, 1.2f, 3.0f); cabin.localPosition = new Vector3(0, 0.7f, -0.25f); }
                else if (loadout.id == "coupe") cabin.localScale = new Vector3(1.5f, 0.4f, 1.6f);
            }
        }

        // Every line spans the road except for one gap, and consecutive lines never share a lane,
        // so the route is a readable slalom rather than a wall of dice rolls.
        void BuildRoadblocks(StageDefinition stage, System.Random rng, Material barrier, Material beacon)
        {
            float[] lanes = { -10.5f, -5.25f, 0, 5.25f, 10.5f };
            float half = Mathf.Clamp(stage.roadblockGap, 5, 16) / 2;
            float spacing = Mathf.Max(25, stage.roadblockSpacing);
            int previous = -1;
            for (float z = Mathf.Max(30, stage.firstRoadblock); z < stage.roadLength - 60; z += spacing)
            {
                if (Mathf.Abs(z - Pickup.z) < 24) continue;
                if (Mathf.Abs(z - JunctionZ) < JunctionHalfWidth + 20) continue;
                int lane = rng.Next(lanes.Length);
                if (lane == previous) lane = (lane + 1 + rng.Next(lanes.Length - 1)) % lanes.Length;
                previous = lane;
                float gap = lanes[lane];
                roadblocks.Add(new Vector2(z, gap));
                XSpan("Roadblock", z, 0.7f, -RoadHalfWidth, gap - half, 2, 1.4f, barrier);
                XSpan("Roadblock", z, 0.7f, gap + half, RoadHalfWidth, 2, 1.4f, barrier);
                Box("Roadblock beacon", root.transform, new Vector3(Mathf.Clamp(gap - half - 1.2f, -RoadHalfWidth, RoadHalfWidth), 1.7f, z), new Vector3(0.5f, 0.5f, 0.5f), beacon, false);
                Box("Roadblock beacon", root.transform, new Vector3(Mathf.Clamp(gap + half + 1.2f, -RoadHalfWidth, RoadHalfWidth), 1.7f, z), new Vector3(0.5f, 0.5f, 0.5f), beacon, false);
            }
        }

        void Label(string text, Vector3 position, Color color)
        {
            var obj = new GameObject(text);
            obj.transform.SetParent(root.transform);
            obj.transform.position = position;
            obj.transform.rotation = Quaternion.Euler(0, 180, 0);
            var label = obj.AddComponent<TextMesh>();
            label.text = text; label.characterSize = 0.5f; label.fontSize = 48;
            label.anchor = TextAnchor.MiddleCenter; label.color = color;
        }
        static void DisablePhysics(GameObject visual)
        {
            foreach (var c in visual.GetComponentsInChildren<Collider>()) { c.enabled = false; Destroy(c); }
            foreach (var rb in visual.GetComponentsInChildren<Rigidbody>()) { rb.isKinematic = true; Destroy(rb); }
        }
        ArcadeCar Car(string name, Vector3 position, Material paint, GameObject visualPrefab)
        {
            var obj = new GameObject(name);
            obj.layer = 8;
            obj.transform.SetParent(root.transform);
            obj.transform.position = position;
            var collider = obj.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.9f, 1.05f, 4);
            collider.sharedMaterial = slippery;
            var visualRoot = new GameObject("Visual").transform;
            visualRoot.SetParent(obj.transform, false);
            if (visualPrefab != null) DisablePhysics(Instantiate(visualPrefab, visualRoot));
            else
            {
                Box("Body", visualRoot, Vector3.zero, new Vector3(1.9f, 0.7f, 4), paint, false);
                Box("Cabin", visualRoot, new Vector3(0, 0.6f, -0.2f), new Vector3(1.6f, 0.6f, 1.8f), Material(new Color(0.15f, 0.22f, 0.27f)), false);
                var rubber = Material(new Color(0.025f, 0.03f, 0.04f));
                for (int side = -1; side <= 1; side += 2)
                    for (int axle = -1; axle <= 1; axle += 2)
                        Box("Wheel", visualRoot, new Vector3(side, -0.25f, axle * 1.2f), new Vector3(0.3f, 0.65f, 0.7f), rubber, false);
                var white = Material(new Color(1, 0.96f, 0.74f));
                Box("Headlights", visualRoot, new Vector3(0, 0.1f, 2.01f), new Vector3(1.5f, 0.18f, 0.05f), white, false);
            }
            return obj.AddComponent<ArcadeCar>();
        }
        public void BoardCrew() { foreach (var member in crew) member.SetActive(false); }
        public void SpawnPolice(StageDefinition stage)
        {
            for (int i = 0; i < stage.policeCount; i++)
            {
                float spawnZ = Player.transform.position.z - 28 - i * 7;
                var car = Car("Police " + (i + 1), new Vector3(i % 2 == 0 ? -4 : 4, 0.8f, spawnZ), Material(new Color(0.8f, 0.84f, 0.9f)), policeVisualPrefab);
                car.topSpeed = stage.policeSpeed;
                car.acceleration = stage.policeAcceleration;
                // Patrol cars keep full grip: a sliding pursuer loses the player instead of pressuring them.
                car.handbrakeDrift = false;
                var ai = car.gameObject.AddComponent<PoliceDriver>(); ai.target = Player; ai.world = this;
                Police.Add(ai);
                Box("Red beacon", car.transform, new Vector3(-0.45f, 1.03f, 0), new Vector3(0.6f, 0.16f, 0.3f), Material(Color.red), false);
                Box("Blue beacon", car.transform, new Vector3(0.45f, 1.03f, 0), new Vector3(0.6f, 0.16f, 0.3f), Material(Color.blue), false);
            }
        }
        public void Clear()
        {
            if (root != null) { root.SetActive(false); Destroy(root); }
            foreach (var mat in materials) if (mat != null) Destroy(mat);
            materials.Clear(); Police.Clear(); crew.Clear(); roadblocks.Clear();
            if (slippery != null) Destroy(slippery);
        }
        void OnDestroy() { Clear(); }
    }
}
