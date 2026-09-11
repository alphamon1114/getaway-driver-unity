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

        // Half width of the drivable surface. Kerb walls sit one metre outside it.
        public const float RoadHalfWidth = 16;
        // Half width in z of the mouth where the escape road leaves the city.
        public const float JunctionHalfWidth = 16;
        // Half width in z of a cross street.
        public const float CrossHalfWidth = 12;
        // How far a cross street runs either side of the main road before it dead-ends.
        public const float SideStreetReach = 70;

        public Vector3 Pickup { get; private set; }
        public Vector3 Destination { get; private set; }
        /// <summary>Centre of the junction where the escape road branches off the main road.</summary>
        public float JunctionZ { get; private set; }
        public ArcadeCar Player { get; private set; }
        public readonly List<PoliceDriver> Police = new List<PoliceDriver>();
        /// <summary>z positions of the cross streets, for anything that needs to avoid a junction.</summary>
        public readonly List<float> CrossStreets = new List<float>();
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
        // Spans clip to nothing when an opening swallows them, which is how kerb walls and
        // barricade lines get their gaps without special cases at the call site.
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
        // Emits a wall between `from` and `to`, skipping every opening. Openings need not be sorted.
        void WallWithOpenings(string label, float x, List<Vector2> openings, float from, float to, Material mat)
        {
            var sorted = new List<Vector2>(openings);
            sorted.Sort((a, b) => a.x.CompareTo(b.x));
            float cursor = from;
            foreach (Vector2 opening in sorted)
            {
                if (opening.y <= cursor) continue;
                if (opening.x >= to) break;
                ZSpan(label, x, 1, cursor, Mathf.Min(opening.x, to), 1, 2, mat);
                cursor = Mathf.Max(cursor, opening.y);
            }
            ZSpan(label, x, 1, cursor, to, 1, 2, mat);
        }

        public void Build(StageDefinition stage, OwnedVehicle loadout = null)
        {
            Clear();
            root = new GameObject("Generated Stage");
            var road = Material(new Color(0.10f, 0.13f, 0.17f));
            var ground = Material(new Color(0.20f, 0.24f, 0.25f));
            var pavement = Material(new Color(0.33f, 0.35f, 0.37f));
            var line = Material(new Color(0.9f, 0.74f, 0.32f));
            var paint = Material(new Color(0.88f, 0.90f, 0.92f));
            var building = Material(new Color(0.28f, 0.35f, 0.42f));
            var building2 = Material(new Color(0.35f, 0.31f, 0.30f));
            var windows = Material(new Color(0.69f, 0.85f, 0.88f));
            var orange = Material(new Color(0.93f, 0.30f, 0.09f));
            var green = Material(new Color(0.17f, 0.78f, 0.52f));
            var blue = Material(new Color(0.10f, 0.55f, 0.9f));
            var barrier = Material(new Color(0.95f, 0.82f, 0.16f));
            var metal = Material(new Color(0.18f, 0.20f, 0.22f));

            float length = stage.roadLength;
            Pickup = new Vector3(0, 0.65f, Mathf.Clamp(stage.bankDistance, 40, length - 160));
            JunctionZ = Mathf.Clamp(length - stage.exitJunctionOffset, Pickup.z + 90, length - 40);
            float exitEndX = RoadHalfWidth + stage.exitRoadLength;
            Destination = new Vector3(exitEndX - 22, 0.65f, JunctionZ);
            slippery = new PhysicsMaterial("Arcade car surface") { dynamicFriction = 0, staticFriction = 0, frictionCombine = PhysicsMaterialCombine.Minimum, bounciness = 0 };

            for (float z = Mathf.Max(50, stage.firstCrossStreet); z < length - 50; z += Mathf.Max(50, stage.crossStreetSpacing))
            {
                if (Mathf.Abs(z - Pickup.z) < 28) continue;
                if (Mathf.Abs(z - JunctionZ) < JunctionHalfWidth + CrossHalfWidth + 12) continue;
                CrossStreets.Add(z);
            }

            Box("Ground", root.transform, new Vector3(0, -0.6f, length / 2), new Vector3(200, 1, length + 160), ground);
            Box("Road", root.transform, new Vector3(0, -0.1f, length / 2), new Vector3(RoadHalfWidth * 2, 0.2f, length + 100), road);

            // Escape road: the highway out of the city, leaving the right-hand side near the top of the map.
            float mouth0 = JunctionZ - JunctionHalfWidth, mouth1 = JunctionZ + JunctionHalfWidth;
            XSpan("Exit ground", JunctionZ, -0.6f, RoadHalfWidth, exitEndX + 50, JunctionHalfWidth * 2 + 90, 1, ground);
            XSpan("Exit road", JunctionZ, -0.1f, RoadHalfWidth - 2, exitEndX, JunctionHalfWidth * 2, 0.2f, road);
            for (int side = -1; side <= 1; side += 2)
                XSpan("Exit boundary", JunctionZ + side * (JunctionHalfWidth + 1), 1, RoadHalfWidth, exitEndX + 6, 1, 2, building);
            Box("City limits gate", root.transform, new Vector3(exitEndX + 5, 1, JunctionZ), new Vector3(1, 2, JunctionHalfWidth * 2), building);

            BuildCrossStreets(road, pavement, paint, metal, building);

            // Kerb walls, opened at every junction so the grid reads as streets rather than a tunnel.
            var leftOpenings = new List<Vector2>();
            var rightOpenings = new List<Vector2> { new Vector2(mouth0, mouth1) };
            foreach (float cz in CrossStreets)
            {
                leftOpenings.Add(new Vector2(cz - CrossHalfWidth, cz + CrossHalfWidth));
                rightOpenings.Add(new Vector2(cz - CrossHalfWidth, cz + CrossHalfWidth));
            }
            WallWithOpenings("Kerb wall", -17, leftOpenings, -50, length + 50, building);
            WallWithOpenings("Kerb wall", 17, rightOpenings, -50, length + 50, building);
            Box("End barrier", root.transform, new Vector3(0, 1, length + 45), new Vector3(35, 2, 1), building);
            Box("Start barrier", root.transform, new Vector3(0, 1, -48), new Vector3(35, 2, 1), building);

            for (float z = -30; z < length + 35; z += 12)
            {
                if (NearCrossStreet(z, CrossHalfWidth + 2)) continue;
                if (Mathf.Abs(z - JunctionZ) < JunctionHalfWidth + 2) continue;
                Box("Lane marking", root.transform, new Vector3(0, 0.025f, z), new Vector3(0.2f, 0.03f, 5), line, false);
            }
            for (float x = RoadHalfWidth + 10; x < exitEndX - 6; x += 12)
                Box("Exit lane marking", root.transform, new Vector3(x, 0.025f, JunctionZ), new Vector3(5, 0.03f, 0.2f), line, false);

            var rng = new System.Random(stage.seed);
            BuildBlocks(stage, rng, building, building2, windows, pavement);
            BuildRoadblocks(stage, rng, barrier, blue);
            BuildParkedCars(stage, rng, metal);
            BuildTraffic(stage, rng);

            Box("Pickup zone", root.transform, new Vector3(0, 0.04f, Pickup.z), new Vector3(12, 0.06f, 12), blue, false);
            Box("Bank", root.transform, new Vector3(30, 6, Pickup.z), new Vector3(16, 12, 24), blue);
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
            Color shade = loadout.id == "coupe" ? new Color(0.8f, 0.12f, 0.20f) : loadout.id == "van" ? new Color(0.18f, 0.55f, 0.4f) : new Color(0.9f, 0.48f, 0.13f);
            GameObject prefab = loadout.id == "coupe" ? coupeVisualPrefab : loadout.id == "van" ? vanVisualPrefab : playerVisualPrefab;
            Player = Car("Player", new Vector3(0, 0.8f, 5), Material(shade), prefab);
            Player.playerControlled = true;
            Player.ApplyLoadout(loadout);
            if (prefab == null)
            {
                var cabin = Player.transform.Find("Visual/Cabin");
                if (loadout.id == "van") { cabin.localScale = new Vector3(1.8f, 1.2f, 3.0f); cabin.localPosition = new Vector3(0, 0.7f, -0.25f); }
                else if (loadout.id == "coupe") cabin.localScale = new Vector3(1.5f, 0.4f, 1.6f);
            }
        }

        bool NearCrossStreet(float z, float margin)
        {
            foreach (float cz in CrossStreets) if (Mathf.Abs(z - cz) < margin) return true;
            return false;
        }

        void BuildCrossStreets(Material road, Material pavement, Material paint, Material metal, Material building)
        {
            foreach (float cz in CrossStreets)
            {
                XSpan("Cross street", cz, -0.1f, -SideStreetReach, SideStreetReach, CrossHalfWidth * 2, 0.2f, road);
                for (int side = -1; side <= 1; side += 2)
                {
                    Box("Side street end", root.transform, new Vector3(side * (SideStreetReach + 1), 1.2f, cz), new Vector3(1, 2.4f, CrossHalfWidth * 2 + 6), building);
                    // Zebra across the main road at both mouths of the junction.
                    for (float x = -14; x <= 14.01f; x += 3.4f)
                        Box("Crosswalk", root.transform, new Vector3(x, 0.03f, cz + side * (CrossHalfWidth + 1.8f)), new Vector3(1.5f, 0.02f, 3.2f), paint, false);
                    // Zebra across the side street just outside the kerb line.
                    for (float z = -10; z <= 10.01f; z += 3.4f)
                        Box("Crosswalk", root.transform, new Vector3(side * (RoadHalfWidth + 3.5f), 0.03f, cz + z), new Vector3(3.2f, 0.02f, 1.5f), paint, false);
                    // Pavement corners, then a signal on each of the four corners.
                    for (int end = -1; end <= 1; end += 2)
                    {
                        Box("Pavement", root.transform, new Vector3(side * 24, 0.06f, cz + end * (CrossHalfWidth + 6)), new Vector3(14, 0.12f, 10), pavement);
                        TrafficSignal(side * (RoadHalfWidth + 2.2f), cz + end * (CrossHalfWidth + 2.2f), metal, paint);
                    }
                }
            }
        }

        void TrafficSignal(float x, float z, Material metal, Material paint)
        {
            Box("Signal pole", root.transform, new Vector3(x, 2.6f, z), new Vector3(0.28f, 5.2f, 0.28f), metal);
            Box("Signal head", root.transform, new Vector3(x, 5.0f, z), new Vector3(0.7f, 1.6f, 0.7f), metal, false);
            Box("Signal lens", root.transform, new Vector3(x, 5.45f, z), new Vector3(0.75f, 0.42f, 0.75f), paint, false);
        }

        // Blocks are the stretches of kerb between junctions. Two deep buildings per block reads as a
        // city while costing a fraction of the objects a building every 28 metres used to.
        void BuildBlocks(StageDefinition stage, System.Random rng, Material a, Material b, Material windows, Material pavement)
        {
            var edges = new List<float> { -40 };
            foreach (float cz in CrossStreets) { edges.Add(cz - CrossHalfWidth - 4); edges.Add(cz + CrossHalfWidth + 4); }
            edges.Add(JunctionZ - JunctionHalfWidth - 4); edges.Add(JunctionZ + JunctionHalfWidth + 4);
            edges.Add(stage.roadLength + 40);
            edges.Sort();
            for (int i = 0; i + 1 < edges.Count; i += 2)
            {
                float from = edges[i], to = edges[i + 1];
                if (to - from < 34) continue;
                for (int side = -1; side <= 1; side += 2)
                {
                    ZSpan("Pavement", side * 19, 0.06f, from, to, 4, 0.12f, pavement);
                    float z = from + 5;
                    while (z + 30 < to)
                    {
                        float depth = 30 + rng.Next(16);
                        if (z + depth > to - 3) depth = to - 3 - z;
                        if (depth < 20) break;
                        // The bank occupies its own corner; do not bury it in an office block.
                        if (side > 0 && Mathf.Abs(z + depth / 2 - Pickup.z) < 26) { z += depth + 11; continue; }
                        float h = 11 + rng.Next(26);
                        Box("City block", root.transform, new Vector3(side * 32, h / 2, z + depth / 2), new Vector3(22, h, depth), rng.Next(2) == 0 ? a : b);
                        for (float y = 4; y < h - 2; y += 6)
                            Box("Window strip", root.transform, new Vector3(side * 20.9f, y, z + depth / 2), new Vector3(0.1f, 1.6f, depth - 6), windows, false);
                        z += depth + 11;
                    }
                }
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
                // Junctions stay clear: cross traffic is the hazard there, not a wall.
                if (NearCrossStreet(z, CrossHalfWidth + 16)) continue;
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

        void BuildParkedCars(StageDefinition stage, System.Random rng, Material metal)
        {
            Color[] paints = { new Color(0.62f, 0.64f, 0.68f), new Color(0.22f, 0.26f, 0.34f), new Color(0.55f, 0.20f, 0.18f), new Color(0.20f, 0.36f, 0.30f) };
            var shades = new Material[paints.Length];
            for (int i = 0; i < paints.Length; i++) shades[i] = Material(paints[i]);
            float density = Mathf.Clamp01(stage.parkedCarDensity);
            for (float z = 40; z < stage.roadLength - 40; z += 16)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    if (rng.NextDouble() > density) continue;
                    if (Mathf.Abs(z - Pickup.z) < 20) continue;
                    if (Mathf.Abs(z - JunctionZ) < JunctionHalfWidth + 8) continue;
                    if (NearCrossStreet(z, CrossHalfWidth + 8)) continue;
                    if (NearRoadblock(z, 14)) continue;
                    Shell("Parked car", new Vector3(side * 13.6f, 0.58f, z), Quaternion.identity, shades[rng.Next(shades.Length)], metal, false);
                }
            }
        }

        bool NearRoadblock(float z, float margin)
        {
            foreach (Vector2 block in roadblocks) if (Mathf.Abs(z - block.x) < margin) return true;
            return false;
        }

        void BuildTraffic(StageDefinition stage, System.Random rng)
        {
            int perStreet = Mathf.Clamp(stage.crossTrafficPerStreet, 0, 3);
            if (perStreet == 0) return;
            var metal = Material(new Color(0.16f, 0.18f, 0.20f));
            Color[] paints = { new Color(0.85f, 0.85f, 0.87f), new Color(0.20f, 0.42f, 0.68f), new Color(0.78f, 0.62f, 0.16f), new Color(0.30f, 0.32f, 0.36f) };
            var shades = new Material[paints.Length];
            for (int i = 0; i < paints.Length; i++) shades[i] = Material(paints[i]);
            foreach (float cz in CrossStreets)
                for (int i = 0; i < perStreet; i++)
                {
                    int heading = i % 2 == 0 ? 1 : -1;
                    // One lane each way, so oncoming traffic passes rather than collides head-on.
                    float laneZ = cz + heading * 5.5f;
                    float startX = -SideStreetReach + 6 + (float)rng.NextDouble() * (SideStreetReach * 2 - 12);
                    var body = Shell("Traffic car", new Vector3(startX, 0.58f, laneZ), Quaternion.Euler(0, heading > 0 ? 90 : -90, 0), shades[rng.Next(shades.Length)], metal, true);
                    var driver = body.AddComponent<TrafficCar>();
                    driver.heading = heading;
                    driver.laneZ = laneZ;
                    driver.speed = 9 + (float)rng.NextDouble() * 5;
                    driver.minX = -SideStreetReach + 3;
                    driver.maxX = SideStreetReach - 3;
                }
        }

        // A plain civilian car: a collider plus two boxes. `moving` adds the rigidbody its driver needs.
        GameObject Shell(string name, Vector3 position, Quaternion rotation, Material paint, Material glass, bool moving)
        {
            var obj = new GameObject(name);
            obj.layer = 8;
            obj.transform.SetParent(root.transform);
            obj.transform.SetPositionAndRotation(position, rotation);
            var collider = obj.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.85f, 1.1f, 4.1f);
            collider.sharedMaterial = slippery;
            Box("Body", obj.transform, Vector3.zero, new Vector3(1.85f, 0.72f, 4.1f), paint, false);
            Box("Cabin", obj.transform, new Vector3(0, 0.58f, -0.1f), new Vector3(1.55f, 0.6f, 1.9f), glass, false);
            if (moving) obj.AddComponent<Rigidbody>();
            return obj;
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
            var shell = Material(new Color(0.92f, 0.93f, 0.95f));
            var stripe = Material(new Color(0.06f, 0.16f, 0.45f));
            var dark = Material(new Color(0.10f, 0.12f, 0.15f));
            for (int i = 0; i < stage.policeCount; i++)
            {
                float spawnZ = Player.transform.position.z - 28 - i * 7;
                var car = Car("Police " + (i + 1), new Vector3(i % 2 == 0 ? -4 : 4, 0.8f, spawnZ), shell, policeVisualPrefab);
                car.topSpeed = stage.policeSpeed;
                car.acceleration = stage.policeAcceleration;
                // Patrol cars keep full grip: a sliding pursuer loses the player instead of pressuring them.
                car.handbrakeDrift = false;
                var ai = car.gameObject.AddComponent<PoliceDriver>(); ai.target = Player; ai.world = this;
                Police.Add(ai);
                Livery(car.transform, stripe, dark);
            }
        }
        // A white shell alone is hard to pick out of grey traffic, so patrols get a dark flank
        // stripe and a raised bar that strobes red and blue.
        void Livery(Transform car, Material stripe, Material dark)
        {
            for (int side = -1; side <= 1; side += 2)
                Box("Livery stripe", car, new Vector3(side * 0.97f, 0.05f, 0), new Vector3(0.05f, 0.34f, 3.4f), stripe, false);
            Box("Light bar mount", car, new Vector3(0, 0.95f, 0.1f), new Vector3(1.5f, 0.12f, 0.34f), dark, false);
            var red = Box("Light bar red", car, new Vector3(-0.42f, 1.12f, 0.1f), new Vector3(0.62f, 0.26f, 0.32f), Material(new Color(1f, 0.12f, 0.12f)), false);
            var blue = Box("Light bar blue", car, new Vector3(0.42f, 1.12f, 0.1f), new Vector3(0.62f, 0.26f, 0.32f), Material(new Color(0.16f, 0.35f, 1f)), false);
            var lights = car.gameObject.AddComponent<PoliceLights>();
            lights.red = red.GetComponent<Renderer>();
            lights.blue = blue.GetComponent<Renderer>();
        }
        public void Clear()
        {
            if (root != null) { root.SetActive(false); Destroy(root); }
            foreach (var mat in materials) if (mat != null) Destroy(mat);
            materials.Clear(); Police.Clear(); crew.Clear(); roadblocks.Clear(); CrossStreets.Clear();
            if (slippery != null) Destroy(slippery);
        }
        void OnDestroy() { Clear(); }
    }
}
