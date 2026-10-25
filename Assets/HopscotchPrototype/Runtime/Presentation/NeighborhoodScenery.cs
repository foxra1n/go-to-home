using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Homeward.Hopscotch
{
    // Decoration never consumes UnityEngine.Random or participates in gameplay collisions.
    // Fixed chunk count and combined meshes keep endless travel bounded.
    public sealed class NeighborhoodScenery : MonoBehaviour
    {
        public const int ChunkCount = 16;
        public const float ChunkLength = 18f;
        private readonly List<Material> palette = new();
        private readonly List<Mesh> meshes = new();
        private readonly Transform[] chunks = new Transform[ChunkCount];
        private Transform target;
        private Material grass, paving, cream, brick, roof, glass, wood, leaf, trim, metal;

        public void Configure(Transform followTarget)
        {
            target = followTarget;
            grass = Make("SageGrass", new Color(.57f, .69f, .43f));
            paving = Make("WarmPaving", new Color(.79f, .76f, .65f));
            cream = Make("SchoolCream", new Color(.94f, .82f, .60f));
            brick = Make("SoftBrick", new Color(.73f, .44f, .32f));
            roof = Make("TerracottaRoof", new Color(.58f, .31f, .24f));
            glass = Make("DustyBlueWindows", new Color(.35f, .59f, .65f));
            wood = Make("TreeBark", new Color(.45f, .32f, .20f));
            leaf = Make("TreeLeaves", new Color(.36f, .56f, .29f));
            trim = Make("IvoryTrim", new Color(.97f, .92f, .77f));
            metal = Make("FenceMetal", new Color(.33f, .42f, .41f));
            for (int i = 0; i < chunks.Length; i++)
            {
                Transform chunk = new GameObject(i == 0 ? "SchoolBlock" : "ResidentialBlock").transform;
                chunk.SetParent(transform, false);
                chunks[i] = chunk;
                BuildBlock(chunk, i);
                Combine(chunk);
            }
            LateUpdate();
        }

        private void LateUpdate()
        {
            if (target == null) return;
            int first = Mathf.FloorToInt(target.position.z / ChunkLength) - 1;
            for (int i = 0; i < chunks.Length; i++)
            {
                int index = first + ((i - first) % ChunkCount + ChunkCount) % ChunkCount;
                chunks[i].localPosition = Vector3.forward * (index * ChunkLength);
            }
        }

        private void BuildBlock(Transform root, int variant)
        {
            // Fill the complete camera footprint, not just the first roadside row.
            // At the top of the perspective view the ground is much farther away.
            Box(root, "NeighborhoodFoundation", 0, -.42f, 9, 240, .26f, 18, paving);
            BuildBackstreets(root, variant);
            foreach (float side in new[] { -1f, 1f })
            {
                Box(root, "Sidewalk", side * 5.1f, -.13f, 9, 1.7f, .22f, 18, paving);
                Box(root, "RoadEdge", side * 4.3f, -.05f, 9, .15f, .12f, 18, trim);
                for (int k = 0; k < 9; k++)
                    Box(root, "PavingJoint", side * 5.1f, -.013f, k * 2, 1.55f, .008f, .025f, trim);
            }
            // Foreground is supplied by SectionScenery at exact gameplay section bounds.
        }

        private void BuildBackstreets(Transform root, int variant)
        {
            for (int row = 0; row < 8; row++)
            {
                float x = -33f - row * 12f;
                // Muted, low-detail blocks give depth without competing with the tiles.
                Box(root, "Backstreet", x + 5f, -.27f, 9, 3.5f, .04f, 18, metal);
                Box(root, "RearGarden", x, -.24f, 9, 7.8f, .08f, 16, grass);
                for (int lot = 0; lot < 2; lot++)
                {
                    float z = 4f + lot * 9f;
                    float height = 2.8f + ((row + lot + variant) % 3) * .8f;
                    Material wall = (row + lot + variant) % 2 == 0 ? cream : brick;
                    Box(root, "DistantHome", x, height * .5f - .2f, z, 6.8f, height, 6.3f, wall);
                    Box(root, "DistantRoof", x, height - .04f, z, 7.1f, .32f, 6.6f, roof);
                    Box(root, "RoofTank", x - 1.4f, height + .35f, z, 1.1f, .5f, 1.1f, trim);
                    for (int window = 0; window < 3; window++)
                        Box(root, "DistantWindow", x + 3.43f, height * .6f, z - 2f + window * 2f,
                            .08f, .9f, .9f, glass);
                }
            }
            // The opposite side also stays filled on wider tablet/PC portrait views.
            for (int row = 0; row < 3; row++)
            {
                float x = 22f + row * 12f;
                Box(root, "SideStreet", x - 5f, -.27f, 9, 3f, .04f, 18, metal);
                House(root, x, 4, brick);
                House(root, x, 13, cream);
            }
        }

        private void School(Transform root)
        {
            Box(root, "SchoolBuilding", -9.1f, 2.25f, 9, 5.6f, 4.7f, 14, cream);
            Box(root, "SchoolBase", -9.1f, .4f, 9, 5.7f, 1f, 14.1f, brick);
            Box(root, "SchoolRoof", -9.1f, 4.68f, 9, 5.95f, .24f, 14.4f, roof);
            Box(root, "SchoolCornice", -6.22f, 2.5f, 9, .14f, .17f, 14, trim);
            for (int floor = 0; floor < 2; floor++)
                for (int window = 0; window < 6; window++)
                {
                    float z = 3.3f + window * 2.2f;
                    Box(root, "SchoolWindowFrame", -6.23f, 1.55f + floor * 1.65f, z, .12f, 1.14f, 1.4f, trim);
                    Box(root, "SchoolWindow", -6.15f, 1.55f + floor * 1.65f, z, .055f, .94f, 1.18f, glass);
                    Box(root, "WindowMullion", -6.11f, 1.55f + floor * 1.65f, z, .04f, .97f, .06f, trim);
                }
            // Open gateway faces the street; no decorative collision or on-screen text.
            for (int k = 0; k < 8; k++)
            {
                if (k == 3 || k == 4) continue;
                float z = 1 + k * 2.2f;
                Box(root, "SchoolWall", -5.95f, .3f, z, .24f, .72f, 2.2f, brick);
                Box(root, "FencePost", -5.95f, .95f, z, .1f, .8f, .1f, metal);
                Box(root, "FenceRail", -5.95f, 1.2f, z, .08f, .08f, 2.2f, metal);
            }
            foreach (float z in new[] { 6.6f, 11f })
                Box(root, "GatePillar", -5.9f, .85f, z, .48f, 1.85f, .48f, cream);
            Box(root, "EntranceDoor", -6.15f, 1, 8.8f, .12f, 2.1f, 2, glass);
            Box(root, "EntranceCanopy", -5.95f, 2.16f, 8.8f, 1, .16f, 2.6f, roof);
            Box(root, "ClockFace", -6.1f, 4.02f, 8.8f, .15f, .8f, .8f, trim);
            Box(root, "ClockHand", -6f, 4.12f, 8.8f, .03f, .25f, .05f, metal);
            Box(root, "ClockHand", -6f, 4.02f, 8.93f, .03f, .05f, .3f, metal);
        }

        private void House(Transform root, float x, float z, Material wall)
        {
            Box(root, "House", x, 1.28f, z, 4.4f, 2.75f, 5.6f, wall);
            Box(root, "RoofEaves", x, 2.73f, z, 4.7f, .25f, 5.9f, roof);
            Box(root, "RoofStep", x, 2.98f, z, 3.7f, .25f, 5.5f, roof);
            Box(root, "RoofRidge", x, 3.23f, z, 2.7f, .25f, 5.1f, roof);
            float face = x + (x < 0 ? 2.23f : -2.23f);
            for (int i = -1; i <= 1; i += 2)
            {
                Box(root, "HouseWindowFrame", face, 1.5f, z + i * 1.7f, .12f, 1.06f, 1.15f, trim);
                Box(root, "HouseWindow", face + (x < 0 ? .07f : -.07f), 1.5f, z + i * 1.7f, .05f, .85f, .94f, glass);
            }
            Box(root, "Door", face, .78f, z, .14f, 1.75f, .85f, wood);
        }

        private void Tree(Transform root, float x, float z, float scale)
        {
            Box(root, "TreeTrunk", x, .8f * scale, z, .3f, 1.8f * scale, .3f, wood);
            Box(root, "TreeCrown", x, 1.9f * scale, z, 1.65f * scale, 1.35f * scale, 1.65f * scale, leaf);
            Box(root, "TreeTop", x - .12f, 2.7f * scale, z, 1.15f * scale, .4f * scale, 1.2f * scale, leaf);
        }

        private static void Box(Transform root, string name, float x, float y, float z, float w, float h, float d, Material material)
            => VoxelStudentVisual.Part(name, root, new Vector3(x, y, z), new Vector3(w, h, d), material);

        private Material Make(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            material.SetFloat("_Smoothness", 0);
            palette.Add(material);
            return material;
        }

        private void Combine(Transform chunk)
        {
            MeshFilter[] parts = chunk.GetComponentsInChildren<MeshFilter>();
            foreach (Material material in palette)
            {
                var instances = new List<CombineInstance>();
                foreach (MeshFilter part in parts)
                    if (part.GetComponent<Renderer>().sharedMaterial == material)
                        instances.Add(new CombineInstance { mesh = part.sharedMesh, transform = chunk.worldToLocalMatrix * part.transform.localToWorldMatrix });
                if (instances.Count == 0) continue;
                var mesh = new Mesh { name = chunk.name + "_" + material.name };
                mesh.CombineMeshes(instances.ToArray());
                meshes.Add(mesh);
                var combined = new GameObject(material.name);
                combined.transform.SetParent(chunk, false);
                combined.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = combined.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }
            foreach (MeshFilter part in parts)
            {
                part.gameObject.SetActive(false);
                Destroy(part.gameObject);
            }
        }

        private void OnDestroy()
        {
            foreach (Mesh mesh in meshes) Destroy(mesh);
            foreach (Material material in palette) Destroy(material);
        }
    }
}
