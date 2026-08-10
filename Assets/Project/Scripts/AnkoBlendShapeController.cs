using UnityEngine;
using TaiyakiKun;

public class AnkoBlendShapeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("あんこの取得数を管理するScoreManager。アサインしない場合は同じオブジェクトから自動取得します。")]
    private ScoreManager scoreManager;

    [Header("Settings")]
    [SerializeField, Min(1), Tooltip("ブレンドシェイプが100になる目標のankoCount")]
    private int targetAnkoCount = 10;

    [SerializeField, Tooltip("変更するブレンドシェイプの名前")]
    private string blendShapeName = "anko";

    // 取得したSkinnedMeshRendererと、それぞれのブレンドシェイプのインデックスをキャッシュする配列
    private SkinnedMeshRenderer[] renderers;
    private int[] blendShapeIndices;

    private void Awake()
    {
        // ScoreManagerがアサインされていない場合は、アタッチされたGameObjectから取得
        if (scoreManager == null)
        {
            scoreManager = GetComponent<ScoreManager>();
        }

        // 子オブジェクトから全てのSkinnedMeshRendererを取得 (trueを指定して非アクティブなオブジェクトも取得対象にする)
        renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        blendShapeIndices = new int[renderers.Length];

        // 各MeshRendererごとにブレンドシェイプのインデックスを検索して保存
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].sharedMesh != null)
            {
                blendShapeIndices[i] = renderers[i].sharedMesh.GetBlendShapeIndex(blendShapeName);
                if (blendShapeIndices[i] == -1)
                {
                    Debug.LogWarning($"[MultiAnkoBlendShapeController] {renderers[i].gameObject.name} にブレンドシェイプ '{blendShapeName}' が見つかりません。", renderers[i]);
                }
            }
            else
            {
                blendShapeIndices[i] = -1;
            }
        }
    }

    private void OnEnable()
    {
        if (scoreManager != null)
        {
            scoreManager.AnkoCountChanged += UpdateBlendShapeWeights;
            UpdateBlendShapeWeights(scoreManager.AnkoCount);
        }
        else
        {
            Debug.LogWarning("[MultiAnkoBlendShapeController] ScoreManagerが見つかりません。", this);
        }
    }

    private void OnDisable()
    {
        if (scoreManager != null)
        {
            scoreManager.AnkoCountChanged -= UpdateBlendShapeWeights;
        }
    }

    /// <summary>
    /// ankoCountに応じてすべての子オブジェクトのブレンドシェイプを更新する
    /// </summary>
    private void UpdateBlendShapeWeights(int currentAnkoCount)
    {
        if (renderers == null || renderers.Length == 0) return;

        // 目標値に対する現在の取得数の割合を計算
        float progress = Mathf.Clamp01((float)currentAnkoCount / targetAnkoCount);

        // ブレンドシェイプのウェイト (0 ～ 100) を計算
        float weight = progress * 100f;

        // 全てのSkinnedMeshRendererに値を適用
        for (int i = 0; i < renderers.Length; i++)
        {
            // オブジェクトが存在し、対象のブレンドシェイプを持っている場合のみ適用
            if (renderers[i] != null && blendShapeIndices[i] != -1)
            {
                renderers[i].SetBlendShapeWeight(blendShapeIndices[i], weight);
            }
        }
    }
}