using System.IO;
using UnityEditor;
using UnityEngine;

namespace Supermarket.EditorTools
{
    /// <summary>
    /// Generates Built-in Standard materials and footprint-sized placeable
    /// prefabs into the project's generic asset folders. Safe to rerun.
    /// </summary>
    public static class SupermarketAssetSetup
    {
        const string Models = "Assets/_Models";
        const string Textures = "Assets/_Textures";
        const string Materials = "Assets/_Materials";
        const string Prefabs = "Assets/_Prefabs/Placeables";

        [MenuItem("Tools/Supermarket Assets/Generate Materials and Prefabs")]
        public static void Generate()
        {
            EnsureFolder(Models);
            EnsureFolder(Textures);
            EnsureFolder(Materials);
            EnsureFolder(Prefabs);

            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogError("Standard shader not found. This package targets Built-in Render Pipeline.");
                return;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Textures }))
            {
                string tp = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(tp);
                string mp = Materials + "/" + name + ".mat";
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
                bool isNew = mat == null;
                if (isNew) mat = new Material(shader);

                mat.shader = shader;
                mat.SetTexture("_MainTex", tex);
                mat.SetFloat("_Glossiness", name == "Glass" ? 0.5f : 0.18f);
                mat.SetFloat("_Metallic", name.Contains("Metal") ? 0.3f : 0f);

                if (isNew) AssetDatabase.CreateAsset(mat, mp);
                else EditorUtility.SetDirty(mat);
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Models }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) continue;

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                instance.name = name;
                Vector3 size = GetSize(name);
                BoxCollider col = instance.GetComponent<BoxCollider>();
                if (col == null) col = instance.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, size.y * 0.5f, 0f);
                col.size = size;
                PrefabUtility.SaveAsPrefabAsset(instance, Prefabs + "/" + name + ".prefab");
                Object.DestroyImmediate(instance);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created Built-in Standard materials and supermarket prefabs in the project asset folders.");
        }

        static Vector3 GetSize(string name)
        {
            switch (name)
            {
                case "Shelf_2x1": return new Vector3(2f, 1.75f, 1f);
                case "Counter_2x1": return new Vector3(2f, 1.05f, 1f);
                case "Cooler_2x1": return new Vector3(2f, 2f, 1f);
                case "Freezer_2x1": return new Vector3(2f, 1.65f, 1f);
                case "Alcohol_Shelf_1x1": return new Vector3(1f, 1.65f, 1f);
                case "Checkout_Lane_4x3": return new Vector3(4f, 1.05f, 3f);
                case "Freezer_4x2": return new Vector3(4f, 1.65f, 2f);
                case "Gondola_14x2": return new Vector3(14f, 1.65f, 2f);
                case "Freezer_6x3": return new Vector3(6f, 1.65f, 3f);
                case "Gondola_16x2": return new Vector3(16f, 1.65f, 2f);
                default: return Vector3.one;
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
