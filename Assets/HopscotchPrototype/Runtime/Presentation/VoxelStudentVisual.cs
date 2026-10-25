using UnityEngine;

namespace Homeward.Hopscotch
{
    // Visuals only: the existing player controller owns all movement and landing poses.
    public sealed class VoxelStudentVisual : MonoBehaviour
    {
        private readonly System.Collections.Generic.List<Material> materials = new();
        private Transform leftArm, rightArm;

        public void SetArms(float spread, float swing = 0f)
        {
            if (leftArm == null) return;
            leftArm.localRotation = Quaternion.Euler(swing, 0, -spread);
            rightArm.localRotation = Quaternion.Euler(-swing, 0, spread);
        }

        public void Build(out Transform torso, out Transform leftFoot, out Transform rightFoot)
        {
            Material shirt = Make("StudentCream", new Color(1f, .91f, .69f));
            Material navy = Make("StudentNavy", new Color(.19f, .27f, .37f));
            Material skin = Make("StudentSkin", new Color(.95f, .68f, .45f));
            Material hair = Make("StudentHair", new Color(.19f, .12f, .09f));
            Material bag = Make("StudentBackpack", new Color(.89f, .27f, .13f));
            Material sole = Make("StudentSoles", new Color(.93f, .91f, .80f));

            torso = new GameObject("Body").transform;
            torso.SetParent(transform, false);
            torso.localPosition = new Vector3(0, 1.05f, 0);
            Part("Shirt", torso, new Vector3(0, .02f, 0), new Vector3(.76f, .67f, .48f), shirt);
            Part("Shorts", torso, new Vector3(0, -.43f, 0), new Vector3(.7f, .3f, .45f), navy);
            Part("Head", torso, new Vector3(0, .77f, .015f), new Vector3(.78f, .72f, .68f), skin);
            Part("HairCap", torso, new Vector3(0, 1.12f, -.015f), new Vector3(.83f, .22f, .73f), hair);
            Part("HairBack", torso, new Vector3(0, .88f, -.31f), new Vector3(.81f, .5f, .14f), hair);
            Part("Fringe", torso, new Vector3(-.18f, 1.01f, .35f), new Vector3(.43f, .23f, .09f), hair);
            foreach (float side in new[] { -1f, 1f })
            {
                Part("Eye", torso, new Vector3(side * .19f, .78f, .363f), new Vector3(.075f, .09f, .03f), navy);
                Transform arm = new GameObject(side < 0 ? "LeftArm" : "RightArm").transform;
                arm.SetParent(torso, false);
                arm.localPosition = new Vector3(side * .47f, .22f, 0);
                if (side < 0) leftArm = arm; else rightArm = arm;
                Part("Sleeve", arm, new Vector3(0, -.17f, 0), new Vector3(.2f, .34f, .38f), shirt);
                Part("Hand", arm, new Vector3(0, -.44f, .025f), new Vector3(.18f, .23f, .28f), skin);
                Part("BagStrap", torso, new Vector3(side * .23f, .03f, .25f), new Vector3(.09f, .62f, .05f), bag);
            }
            Part("Backpack", torso, new Vector3(0, -.01f, -.36f), new Vector3(.62f, .66f, .27f), bag);
            Part("BagPocket", torso, new Vector3(0, -.15f, -.52f), new Vector3(.44f, .28f, .08f), bag);
            Part("BagBuckle", torso, new Vector3(0, -.1f, -.57f), new Vector3(.12f, .08f, .025f), sole);
            leftFoot = Foot("LeftFoot", navy, sole, skin);
            rightFoot = Foot("RightFoot", navy, sole, skin);
        }

        private Transform Foot(string label, Material shoe, Material sole, Material skin)
        {
            Transform foot = new GameObject(label).transform;
            foot.SetParent(transform, false);
            Part("Shoe", foot, Vector3.zero, new Vector3(.28f, .2f, .48f), shoe);
            Part("Sole", foot, new Vector3(0, -.085f, .015f), new Vector3(.29f, .06f, .5f), sole);
            Part("Sock", foot, new Vector3(0, .18f, -.05f), new Vector3(.22f, .22f, .23f), sole);
            Part("Leg", foot, new Vector3(0, .34f, -.05f), new Vector3(.22f, .15f, .23f), skin);
            return foot;
        }

        internal static void Part(string label, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = label;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
        }

        private Material Make(string label, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = label, color = color };
            material.SetFloat("_Smoothness", 0f);
            materials.Add(material);
            return material;
        }

        private void OnDestroy()
        {
            foreach (Material material in materials) Destroy(material);
        }
    }
}
