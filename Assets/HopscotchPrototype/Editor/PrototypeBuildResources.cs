using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Homeward.Hopscotch.Editor
{
    // Shader.Find strings are not asset references. Resources materials make these
    // shaders explicit build dependencies, including builds started from the Editor UI.
    public sealed class PrototypeBuildResources : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report) => EnsureIncluded();

        public static void EnsureIncluded()
        {
            const string root = "Assets/HopscotchPrototype";
            if (!AssetDatabase.IsValidFolder(root + "/Resources"))
                AssetDatabase.CreateFolder(root, "Resources");
            if (!AssetDatabase.IsValidFolder(root + "/Resources/PrototypeMaterials"))
                AssetDatabase.CreateFolder(root + "/Resources", "PrototypeMaterials");
            Include("Lit", "Universal Render Pipeline/Lit");
            Include("Cue", "Universal Render Pipeline/Particles/Unlit");
            AssetDatabase.SaveAssets();
        }

        private static void Include(string name, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new BuildFailedException("Required shader missing: " + shaderName);
            string path = "Assets/HopscotchPrototype/Resources/PrototypeMaterials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                AssetDatabase.CreateAsset(new Material(shader) { name = name }, path);
            else if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
        }
    }
}
