using System;
using TaiyakiKun;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TaiyakiKun.Editor
{
    /// <summary>
    /// hyodoの操作系とAnkoCollectionPlaygroundの収集物をmerge_cont_billへ統合します。
    /// </summary>
    public static class MergeContBillSceneBuilder
    {
        private const string SourceScenePath = "Assets/Project/Scenes/Test/hyodo.unity";
        private const string DestinationScenePath = "Assets/Project/Scenes/Test/merge_cont_bill.unity";
        private const string TaiyakiSourceScenePath =
            "Assets/Project/Scenes/Test/merge_cont_bill.unity";
        private const string TaiyakiDestinationScenePath =
            "Assets/Project/Scenes/Test/merge_cont_bill_taiyaki.unity";
        private const string TaiyakiModelPath =
            "Assets/Project/Models/Taiyaki/TAIYAKI.fbx";
        private const string AnkoPrefabPath = "Assets/Project/Prefabs/AnkoBillboard.prefab";
        private const float TaiyakiVisualLength = 1.6f;

        private static readonly Vector3[] PickupOffsets =
        {
            new Vector3(-3f, 0f, -2.4f),
            new Vector3(0f, 0f, -1.4f),
            new Vector3(3f, 0f, -2.4f),
            new Vector3(-2.5f, 0f, 1.2f),
            new Vector3(0f, 0f, 2.8f),
            new Vector3(2.5f, 0f, 1.2f),
        };

        [MenuItem("Tools/Taiyaki-kun/Rebuild merge_cont_bill")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            GameObject player = FindRoot(scene, "Taiyaki_test");
            if (player == null)
            {
                throw new InvalidOperationException("hyodo sceneにTaiyaki_testが見つかりません。");
            }

            GameObject ankoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AnkoPrefabPath);
            if (ankoPrefab == null)
            {
                throw new InvalidOperationException($"Prefabが見つかりません: {AnkoPrefabPath}");
            }

            ScoreManager scoreManager = player.GetComponent<ScoreManager>();
            if (scoreManager == null)
            {
                scoreManager = player.AddComponent<ScoreManager>();
            }

            if (player.GetComponent<FishAnkoProgression>() == null)
            {
                player.AddComponent<FishAnkoProgression>();
            }

            if (player.GetComponent<AnkoCollectionFeedback>() == null)
            {
                player.AddComponent<AnkoCollectionFeedback>();
            }

            Vector3 playerStart = player.transform.position;
            for (int i = 0; i < PickupOffsets.Length; i++)
            {
                GameObject pickup = (GameObject)PrefabUtility.InstantiatePrefab(ankoPrefab, scene);
                pickup.name = $"AnkoBillboard_{i + 1}";
                pickup.transform.position = new Vector3(
                    playerStart.x + PickupOffsets[i].x,
                    1.2f,
                    playerStart.z + PickupOffsets[i].z);
                pickup.transform.rotation = Quaternion.identity;
                pickup.transform.localScale = Vector3.one * 1.8f;
            }

            if (!EditorSceneManager.SaveScene(scene, DestinationScenePath, false))
            {
                throw new InvalidOperationException($"Sceneを保存できませんでした: {DestinationScenePath}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "merge_cont_billを再構築しました: FishHopper + XZFollowCamera + " +
                "AnkoBillboard x6 + ScoreManager + FishAnkoProgression");
        }

        [MenuItem("Tools/Taiyaki-kun/Rebuild merge_cont_bill_taiyaki")]
        public static void BuildTaiyakiVariant()
        {
            Scene scene = EditorSceneManager.OpenScene(
                TaiyakiSourceScenePath,
                OpenSceneMode.Single);
            GameObject player = FindRoot(scene, "Taiyaki_test");
            if (player == null)
            {
                throw new InvalidOperationException(
                    "merge_cont_bill sceneにTaiyaki_testが見つかりません。");
            }

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(TaiyakiModelPath);
            if (modelAsset == null)
            {
                throw new InvalidOperationException($"Modelが見つかりません: {TaiyakiModelPath}");
            }

            // Keep physics and gameplay scripts on the root, replacing only the
            // temporary cube graphics with the imported fish model.
            MeshRenderer temporaryRenderer = player.GetComponent<MeshRenderer>();
            if (temporaryRenderer != null)
            {
                UnityEngine.Object.DestroyImmediate(temporaryRenderer);
            }

            MeshFilter temporaryMesh = player.GetComponent<MeshFilter>();
            if (temporaryMesh != null)
            {
                UnityEngine.Object.DestroyImmediate(temporaryMesh);
            }

            Transform existingVisual = player.transform.Find("TaiyakiVisual");
            if (existingVisual != null)
            {
                UnityEngine.Object.DestroyImmediate(existingVisual.gameObject);
            }

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, scene);
            visual.name = "TaiyakiVisual";
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            visual.transform.localScale = Vector3.one;

            FitVisualToPlayer(visual.transform, player.transform, TaiyakiVisualLength);

            FishHopper fishHopper = player.GetComponent<FishHopper>();
            if (fishHopper == null)
            {
                throw new InvalidOperationException("Taiyaki_testにFishHopperがありません。");
            }

            SerializedObject hopperObject = new SerializedObject(fishHopper);
            hopperObject.FindProperty("visualRoot").objectReferenceValue = visual.transform;
            hopperObject.ApplyModifiedPropertiesWithoutUndo();

            FitColliderToVisual(player, visual.transform);
            player.name = "Taiyaki";

            if (!EditorSceneManager.SaveScene(scene, TaiyakiDestinationScenePath, false))
            {
                throw new InvalidOperationException(
                    $"Sceneを保存できませんでした: {TaiyakiDestinationScenePath}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "merge_cont_bill_taiyakiを再構築しました: " +
                "FishHopper root + TAIYAKI model + Anko collection");
        }

        private static void FitVisualToPlayer(
            Transform visual,
            Transform player,
            float targetLongestSide)
        {
            Bounds bounds = CalculateBoundsInSpace(visual, player);
            float longestSide = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (longestSide <= Mathf.Epsilon)
            {
                throw new InvalidOperationException("TAIYAKI modelのRenderer boundsを取得できません。");
            }

            visual.localScale *= targetLongestSide / longestSide;
            bounds = CalculateBoundsInSpace(visual, player);
            visual.localPosition -= bounds.center;
        }

        private static void FitColliderToVisual(GameObject player, Transform visual)
        {
            BoxCollider collider = player.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = player.AddComponent<BoxCollider>();
            }

            Bounds bounds = CalculateBoundsInSpace(visual, player.transform);
            Vector3 size = bounds.size;
            size.x = Mathf.Max(size.x, 0.45f);
            size.y = Mathf.Max(size.y, 0.65f);
            size.z = Mathf.Max(size.z, 0.45f);
            collider.center = bounds.center;
            collider.size = size;
        }

        private static Bounds CalculateBoundsInSpace(Transform visual, Transform space)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            bool hasPoint = false;
            Bounds bounds = new Bounds();

            foreach (Renderer renderer in renderers)
            {
                Bounds localBounds = renderer.localBounds;
                Vector3 min = localBounds.min;
                Vector3 max = localBounds.max;

                for (int x = 0; x <= 1; x++)
                {
                    for (int y = 0; y <= 1; y++)
                    {
                        for (int z = 0; z <= 1; z++)
                        {
                            Vector3 localPoint = new Vector3(
                                x == 0 ? min.x : max.x,
                                y == 0 ? min.y : max.y,
                                z == 0 ? min.z : max.z);
                            Vector3 point = space.InverseTransformPoint(
                                renderer.transform.TransformPoint(localPoint));

                            if (!hasPoint)
                            {
                                bounds = new Bounds(point, Vector3.zero);
                                hasPoint = true;
                            }
                            else
                            {
                                bounds.Encapsulate(point);
                            }
                        }
                    }
                }
            }

            return bounds;
        }

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                {
                    return root;
                }
            }

            return null;
        }
    }
}
