using System;
using System.Linq;
using ToxicDepths.Mining;
using ToxicDepths.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ToxicDepths.Editor
{
    public static class MiningSceneSetup
    {
        [MenuItem("Tools/Toxic Depths/Abbauen in aktueller Szene einrichten")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Abbau nur ausserhalb des Play-Modus einrichten.");

            var scene = SceneManager.GetActiveScene();
            var movement = scene.GetRootGameObjects().SelectMany(root =>
                root.GetComponentsInChildren<PlayerMovement>(true)).FirstOrDefault();
            if (movement == null) throw new InvalidOperationException("PlayerMovement fehlt in der Szene.");
            var movementData = new SerializedObject(movement);
            var camera = movementData.FindProperty("playerCamera").objectReferenceValue as Camera;
            var heldItem = movementData.FindProperty("firstPersonHeldItem").objectReferenceValue as GameObject;
            var crosshair = UnityEngine.Object.FindAnyObjectByType<DynamicCrosshair>();
            if (camera == null || heldItem == null || crosshair == null)
                throw new InvalidOperationException("Spielerkamera, gehaltene Spitzhacke und DynamicCrosshair zuweisen.");

            var mining = movement.GetComponent<PlayerMining>();
            if (mining == null) mining = Undo.AddComponent<PlayerMining>(movement.gameObject);
            Assign(mining, "playerCamera", camera);
            Assign(mining, "heldPickaxe", heldItem.transform);
            Assign(mining, "hotbar", UnityEngine.Object.FindAnyObjectByType<HotbarController>());
            Assign(mining, "crosshair", crosshair);
            var handMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Prefabs/Pilzling_RightHand_Static.asset");
            var hand = heldItem.GetComponentsInChildren<MeshFilter>(true)
                .FirstOrDefault(filter => handMesh != null && filter.sharedMesh == handMesh);
            Assign(mining, "carryHand", hand);
            Assign(mining, "oreOutlineMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Assets/ores/OreOutline.mat"));
            foreach (var part in heldItem.GetComponentsInChildren<Transform>(true))
            {
                Undo.RecordObject(part.gameObject, "Werkzeug vom Abbau-Raycast ausschliessen");
                part.gameObject.layer = Physics.IgnoreRaycastLayer;
            }

            int nodes = 0;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                string asset = AssetDatabase.GetAssetPath(filter.sharedMesh);
                if (!asset.Contains("/ores/emerald_ore/")) continue;
                var instance = PrefabUtility.GetOutermostPrefabInstanceRoot(filter.gameObject) ?? filter.gameObject;
                if (instance.GetComponent<OreDeposit>() != null) continue;
                Undo.AddComponent<OreDeposit>(instance);
                var collider = filter.GetComponent<MeshCollider>();
                if (collider == null) collider = Undo.AddComponent<MeshCollider>(filter.gameObject);
                Undo.RecordObject(collider, "Erzkollision einrichten");
                collider.sharedMesh = filter.sharedMesh;
                collider.isTrigger = false;
                collider.enabled = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
                nodes++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Abbau eingerichtet: {nodes} neue Erzvorkommen, Spitzhacke, Energie und DynamicCrosshair verbunden.");
        }

        private static void Assign(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }
    }
}
