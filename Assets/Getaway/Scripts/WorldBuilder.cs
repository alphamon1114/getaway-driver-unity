using System.Collections.Generic;
using UnityEngine;

namespace Getaway
{
    public sealed class WorldBuilder : MonoBehaviour
    {
        public GameObject playerVisualPrefab;
        public GameObject policeVisualPrefab;
        public GameObject crewVisualPrefab;
        public Vector3 Pickup => new Vector3(0, 0.65f, 32);
        public Vector3 Destination { get; private set; }
        public ArcadeCar Player { get; private set; }
        public readonly List<PoliceDriver> Police = new List<PoliceDriver>();
        readonly List<GameObject> crew = new List<GameObject>();
        readonly List<Material> materials = new List<Material>();
        GameObject root;
        PhysicsMaterial slippery;

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

        public void Build(StageDefinition stage)
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
            float length = stage.roadLength;
            Destination = new Vector3(0, 0.65f, length - 25);
            slippery = new PhysicsMaterial("Arcade car surface") { dynamicFriction = 0, staticFriction = 0, frictionCombine = PhysicsMaterialCombine.Minimum, bounciness = 0 };
            Box("Ground", root.transform, new Vector3(0, -0.6f, length / 2), new Vector3(160, 1, length + 160), ground);
            Box("Road", root.transform, new Vector3(0, -0.1f, length / 2), new Vector3(32, 0.2f, length + 100), road);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Road boundary", root.transform, new Vector3(side * 17, 1, length / 2), new Vector3(1, 2, length + 100), building);
                Box("Edge stripe", root.transform, new Vector3(side * 15, 0.02f, length / 2), new Vector3(0.15f, 0.02f, length + 90), line, false);
            }
            Box("End barrier", root.transform, new Vector3(0, 1, length + 45), new Vector3(35, 2, 1), building);
            Box("Start barrier", root.transform, new Vector3(0, 1, -48), new Vector3(35, 2, 1), building);
            for (int z = -30; z < length + 35; z += 12)
                Box("Lane marking", root.transform, new Vector3(0, 0.025f, z), new Vector3(0.2f, 0.03f, 5), line, false);
            var rng = new System.Random(stage.seed);
            for (int z = 0; z < length; z += 28)
                for (int side = -1; side <= 1; side += 2)
                {
                    float h = 9 + rng.Next(20);
                    Box("City block", root.transform, new Vector3(side * 30, h / 2, z), new Vector3(18, h, 21), building);
                    for (int y = 3; y < h - 1; y += 4)
                        Box("Window strip", root.transform, new Vector3(side * 20.9f, y, z), new Vector3(0.1f, 1.4f, 15), windows, false);
                }
            for (int z = 125; z < length - 90; z += 85)
            {
                float x = ((z / 85) % 2 == 0 ? -1 : 1) * 7;
                Box("Roadworks", root.transform, new Vector3(x, 0.7f, z), new Vector3(8, 1.4f, 2), orange);
            }
            Box("Pickup zone", root.transform, new Vector3(0, 0.04f, Pickup.z), new Vector3(12, 0.06f, 12), blue, false);
            Box("Safehouse zone", root.transform, new Vector3(0, 0.04f, Destination.z), new Vector3(14, 0.06f, 14), green, false);
            Label("CREW PICKUP", new Vector3(0, 6, Pickup.z), blue.color);
            Label("SAFEHOUSE", new Vector3(0, 7, Destination.z), green.color);
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
            Player = Car("Player", new Vector3(0, 0.8f, 5), Material(new Color(0.9f, 0.48f, 0.13f)), playerVisualPrefab);
            Player.playerControlled = true;
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
                var car = Car("Police " + (i + 1), new Vector3(i % 2 == 0 ? -4 : 4, 0.8f, -5 - i * 7), Material(new Color(0.8f, 0.84f, 0.9f)), policeVisualPrefab);
                car.topSpeed = stage.policeSpeed;
                // Patrol cars keep full grip: a sliding pursuer loses the player instead of pressuring them.
                car.handbrakeDrift = false;
                var ai = car.gameObject.AddComponent<PoliceDriver>(); ai.target = Player;
                Police.Add(ai);
                Box("Red beacon", car.transform, new Vector3(-0.45f, 1.03f, 0), new Vector3(0.6f, 0.16f, 0.3f), Material(Color.red), false);
                Box("Blue beacon", car.transform, new Vector3(0.45f, 1.03f, 0), new Vector3(0.6f, 0.16f, 0.3f), Material(Color.blue), false);
            }
        }
        public void Clear()
        {
            if (root != null) { root.SetActive(false); Destroy(root); }
            foreach (var mat in materials) if (mat != null) Destroy(mat);
            materials.Clear(); Police.Clear(); crew.Clear();
            if (slippery != null) Destroy(slippery);
        }
        void OnDestroy() { Clear(); }
    }
}
