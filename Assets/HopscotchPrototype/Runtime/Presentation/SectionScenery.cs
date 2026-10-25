using System.Collections.Generic;
using UnityEngine;

namespace Homeward.Hopscotch
{
    public enum SceneryTheme { Residential, School, Road }

    // Listens to section preparation, not input or scoring. All art is decoration only.
    public sealed class SectionScenery : MonoBehaviour
    {
        private const float TemplateLength = 30f;
        private readonly List<Material> materials = new();
        private readonly List<Mesh> meshes = new();
        private readonly Transform[,] pool = new Transform[3, 2];
        private readonly Queue<Section> active = new();
        private GameModeSequenceController sequence;
        private int neighborhoodIndex;
        private Transform initialSidewalk;
        private Material cream, brick, roof, glass, fence, sand, white, asphalt, yellow, green;
        private struct Section { public Transform root; public SceneryTheme theme; public float start, end; }
        public int ActiveSectionCount => active.Count;

        public void Configure(GameModeSequenceController source)
        {
            sequence = source;
            cream = Mat("CampusCream", new Color(.94f,.84f,.66f));
            brick = Mat("ResidentialBrick", new Color(.75f,.45f,.32f));
            roof = Mat("WarmRoof", new Color(.58f,.31f,.24f));
            glass = Mat("BlueGlass", new Color(.32f,.56f,.64f));
            fence = Mat("IronFence", new Color(.22f,.29f,.30f));
            sand = Mat("SchoolSand", new Color(.88f,.73f,.46f));
            white = Mat("RoadPaint", new Color(.94f,.93f,.83f));
            asphalt = Mat("Asphalt", new Color(.23f,.27f,.29f));
            yellow = Mat("LaneYellow", new Color(.97f,.72f,.19f));
            green = Mat("SignalGreen", new Color(.29f,.67f,.40f));
            initialSidewalk = new GameObject("InitialCrosswalkSidewalk").transform;
            initialSidewalk.SetParent(transform, false);
            Box(initialSidewalk,"SidewalkApron",0,-.03f,-2.5f,39,.18f,5,cream);
            Box(initialSidewalk,"CurbLip",0,.015f,-.09f,39,.08f,.18f,white);
            Box(initialSidewalk,"TactilePaving",0,.068f,-.48f,5.8f,.014f,.5f,yellow);
            initialSidewalk.gameObject.SetActive(false);
            for (int theme = 0; theme < 3; theme++)
            {
                Transform template = new GameObject(((SceneryTheme)theme).ToString()).transform;
                template.SetParent(transform, false);
                if (theme == 0) Residential(template);
                else if (theme == 1) School(template);
                else Road(template);
                Combine(template);
                template.gameObject.SetActive(false);
                pool[theme, 0] = template;
                Transform copy = new GameObject(template.name + "Pooled").transform;
                copy.SetParent(transform, false);
                // Source cubes await end-of-frame destruction. Clone only the merged renderers.
                foreach (Transform child in template)
                    if (child.gameObject.activeSelf) Instantiate(child, copy);
                copy.gameObject.SetActive(false);
                pool[theme, 1] = copy;
            }
            if (sequence != null)
            {
                sequence.RunReset += ResetSections;
                sequence.SectionPrepared += PrepareSection;
            }
            ResetSections();
            PrepareSection(PrototypeGameMode.Hopscotch, 0, 27);
            PrepareSection(PrototypeGameMode.CurbBalance, 30, 60);
        }

        public void ResetSections()
        {
            while (active.Count > 0) active.Dequeue().root.gameObject.SetActive(false);
            neighborhoodIndex = 0;
            initialSidewalk.gameObject.SetActive(false);
        }

        public void PrepareSection(PrototypeGameMode mode, float startZ, float endZ, float surfaceStartZ = float.NaN)
        {
            SceneryTheme theme = mode == PrototypeGameMode.Crosswalk ? SceneryTheme.Road
                : neighborhoodIndex++ % 2 == 0 ? SceneryTheme.Residential : SceneryTheme.School;
            // Keep previous/current/next together; recycle only the oldest, behind the player.
            if (active.Count == 3) active.Dequeue().root.gameObject.SetActive(false);
            Transform slot = null;
            for (int i = 0; i < 2; i++)
                if (!pool[(int)theme, i].gameObject.activeSelf) { slot = pool[(int)theme, i]; break; }
            if (slot == null) throw new System.InvalidOperationException("Scenery pool exhausted");
            float start = active.Count == 0 ? startZ - 5f : startZ;
            if (active.Count == 0 && mode == PrototypeGameMode.Crosswalk)
            {
                start = float.IsNaN(surfaceStartZ) ? startZ : surfaceStartZ;
                initialSidewalk.localPosition = Vector3.forward * start;
                initialSidewalk.gameObject.SetActive(true);
            }
            float end = Mathf.Max(start + 1, endZ + 3f);
            slot.localPosition = Vector3.forward * start;
            slot.localScale = new Vector3(1, 1, (end - start) / TemplateLength);
            slot.gameObject.SetActive(true);
            active.Enqueue(new Section { root = slot, theme = theme, start = start, end = end });
        }

        public SceneryTheme? ThemeAt(float z)
        {
            foreach (Section section in active)
                if (z >= section.start && z < section.end) return section.theme;
            return null;
        }

        private void Residential(Transform root)
        {
            for (int i = 0; i < 3; i++)
            {
                float z = 4.5f + i * 10f;
                House(root, -9, z, i % 2 == 0 ? brick : cream);
                House(root, -20, z, cream);
                House(root, -27, z, brick);
                House(root, 13, z, cream);
                Box(root, "GardenWall", -6, .35f, z, .22f, .8f, 9, cream);
                Box(root, "GardenTree", -5.4f, .7f, z + 3, .25f, 1.5f, .25f, roof);
                Box(root, "TreeCrown", -5.4f, 1.8f, z + 3, 1.3f, 1.2f, 1.3f, green);
            }
        }

        private void House(Transform r, float x, float z, Material wall)
        {
            Box(r,"House",x,1.3f,z,5,2.8f,7,wall);
            Box(r,"Roof",x,2.8f,z,5.4f,.25f,7.4f,roof);
            Box(r,"RoofStep",x,3.1f,z,4.2f,.35f,7,roof);
            float face = x + (x < 0 ? 2.53f : -2.53f);
            for(int i=-1;i<=1;i+=2) Box(r,"Window",face,1.6f,z+i*2,.08f,1.05f,1.2f,glass);
            Box(r,"Door",face,.85f,z,.09f,1.8f,.9f,fence);
        }

        private void School(Transform r)
        {
            // Road -> iron fence -> open sand yard -> large three-storey school.
            Box(r,"SandPlayground",-8.8f,-.08f,15,7.4f,.18f,29.8f,sand);
            Box(r,"SchoolBuilding",-16,3.15f,15,7,6.6f,28,cream);
            Box(r,"SchoolFoundation",-16,.35f,15,7.15f,.8f,28.2f,brick);
            Box(r,"SchoolRoof",-16,6.55f,15,7.4f,.26f,28.4f,roof);
            for(int floor=0;floor<3;floor++)
            {
                Box(r,"FloorBand",-12.43f,1.95f+floor*2,15,.14f,.16f,28,white);
                for(int window=0;window<11;window++)
                {
                    float z=2.5f+window*2.5f;
                    Box(r,"WindowFrame",-12.4f,1.2f+floor*2,z,.14f,1.25f,1.65f,white);
                    Box(r,"Window",-12.3f,1.2f+floor*2,z,.08f,1.03f,1.4f,glass);
                }
            }
            Box(r,"SchoolEntrance",-12.25f,1.2f,15,.18f,2.6f,3.4f,glass);
            Box(r,"EntranceCanopy",-11.8f,2.65f,15,1.6f,.18f,4.2f,roof);
            Box(r,"Clock",-12.2f,5.6f,15,.2f,1.2f,1.2f,white);
            Box(r,"ClockHand",-12.06f,5.75f,15,.05f,.45f,.08f,fence);
            Box(r,"ClockHand",-12.06f,5.6f,15.2f,.05f,.08f,.45f,fence);
            for(int i=0;i<=60;i++)
            {
                float z=i*.5f;
                if(z>13.5f && z<16.5f) continue;
                Box(r,"IronBar",-4.95f,.78f,z,.075f,1.65f,.075f,fence);
            }
            foreach(float z in new[]{6.75f,23.25f})
            {
                Box(r,"FenceRail",-4.95f,1.5f,z,.095f,.09f,13.5f,fence);
                Box(r,"FenceRail",-4.95f,.38f,z,.095f,.09f,13.5f,fence);
            }
            foreach(float z in new[]{13.5f,16.5f}) Box(r,"GatePillar",-4.95f,.85f,z,.38f,1.9f,.38f,cream);
            // Two goal frames and white field lines make the empty yard read as a school ground.
            foreach(float z in new[]{4f,26f})
            {
                Box(r,"GoalPost",-6.8f,1,z,.09f,2,.09f,white);
                Box(r,"GoalPost",-10.8f,1,z,.09f,2,.09f,white);
                Box(r,"GoalCrossbar",-8.8f,2,z,4,.09f,.09f,white);
            }
            foreach(float x in new[]{-6.4f,-11.2f}) Box(r,"FieldLine",x,.025f,15,.08f,.015f,22,white);
            Box(r,"FieldLine",-8.8f,.025f,15,4.8f,.015f,.08f,white);
            foreach(float z in new[]{8f,22f})
            {
                Box(r,"GardenTree",-5.8f,.8f,z,.3f,1.6f,.3f,roof);
                Box(r,"TreeCrown",-5.8f,2f,z,1.5f,1.4f,1.5f,green);
            }
        }

        private void Road(Transform r)
        {
            Box(r,"RoadSurface",0,-.015f,15,39,.03f,30,asphalt);
            foreach(float side in new[]{-1f,1f})
            {
                // Keep all lane paint away from the white playable stripe corridor.
                Box(r,"StopLine",side*4,.007f,15,.16f,.012f,28,white);
                for(int i=0;i<5;i++)
                    Box(r,"LaneDash",side*(6+i*2.7f),.007f,15,1.6f,.012f,.13f,yellow);
                Box(r,"RoadsidePaving",side*20,-.025f,15,2,.2f,30,cream);
                // Live CrosswalkSignal owns pedestrian lights; static green lamps
                // would contradict the red wait at the same crossing.
                Car(r,side*8,7,side<0?brick:glass);
                Car(r,side*13,23,side<0?cream:yellow);
            }
        }

        private void Signal(Transform r,float x,float z)
        {
            Box(r,"SignalPole",x,1.1f,z,.12f,2.3f,.12f,fence);
            Box(r,"SignalBox",x,2.2f,z,.42f,.8f,.28f,fence);
            Box(r,"GreenSignal",x,2.06f,z-.16f,.23f,.23f,.045f,green);
            Box(r,"RedSignal",x,2.42f,z-.16f,.23f,.23f,.045f,brick);
        }

        private void Car(Transform r,float x,float z,Material paint)
        {
            Box(r,"ParkedCar",x,.48f,z,3.2f,.65f,1.45f,paint);
            Box(r,"CarCabin",x,.99f,z,1.7f,.52f,1.25f,glass);
            Box(r,"CarRoof",x,1.29f,z,1.8f,.12f,1.3f,paint);
            foreach(float dx in new[]{-1f,1f})
                foreach(float dz in new[]{-.7f,.7f}) Box(r,"Wheel",x+dx,.25f,z+dz,.5f,.48f,.16f,fence);
            Box(r,"Headlamp",x-1.63f,.53f,z,.06f,.18f,1.04f,white);
        }

        private void Box(Transform r,string label,float x,float y,float z,float w,float h,float d,Material m)
        {
            if(label == "TreeCrown")
            {
                // Each crown follows its cube trunk; reuse its mesh without creating a collider.
                Mesh cube = r.GetChild(r.childCount - 1).GetComponent<MeshFilter>().sharedMesh;
                var crown = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
                crown.transform.SetParent(r, false);
                crown.transform.localPosition = new Vector3(x,y,z);
                crown.transform.localScale = new Vector3(w,h,d);
                crown.GetComponent<MeshFilter>().sharedMesh = cube;
                crown.GetComponent<MeshRenderer>().sharedMaterial = m;
                crown.AddComponent<WindTreeSway>();
                return;
            }
            VoxelStudentVisual.Part(label,r,new Vector3(x,y,z),new Vector3(w,h,d),m);
        }
        private Material Mat(string label,Color color)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=label,color=color};
            m.SetFloat("_Smoothness",0); materials.Add(m); return m;
        }
        private void Combine(Transform root)
        {
            MeshFilter[] parts=root.GetComponentsInChildren<MeshFilter>();
            foreach(Material material in materials)
            {
                var instances=new List<CombineInstance>();
                foreach(MeshFilter part in parts)
                    if(part.GetComponent<WindTreeSway>() == null && part.GetComponent<Renderer>().sharedMaterial==material)
                        instances.Add(new CombineInstance{mesh=part.sharedMesh,transform=root.worldToLocalMatrix*part.transform.localToWorldMatrix});
                if(instances.Count==0) continue;
                var mesh=new Mesh{name=root.name+"_"+material.name};
                mesh.CombineMeshes(instances.ToArray()); meshes.Add(mesh);
                var combined=new GameObject(material.name); combined.transform.SetParent(root,false);
                combined.AddComponent<MeshFilter>().sharedMesh=mesh;
                combined.AddComponent<MeshRenderer>().sharedMaterial=material;
            }
            foreach(MeshFilter part in parts)
                if(part.GetComponent<WindTreeSway>() == null){part.gameObject.SetActive(false);Destroy(part.gameObject);}
        }
        private void OnDestroy()
        {
            if(sequence!=null){sequence.RunReset-=ResetSections;sequence.SectionPrepared-=PrepareSection;}
            foreach(Mesh mesh in meshes) Destroy(mesh);
            foreach(Material material in materials) Destroy(material);
        }
    }
}
