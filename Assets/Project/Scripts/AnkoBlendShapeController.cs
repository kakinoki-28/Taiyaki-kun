using UnityEngine;
using TaiyakiKun;

public class AnkoBlendShapeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("あんこの取得数を管理するScoreManager。")]
    private ScoreManager scoreManager;

    [SerializeField, Tooltip("対象のSkinnedMeshRenderer。")]
    private SkinnedMeshRenderer targetRenderer;

    [Header("Settings")]
    [SerializeField, Min(1), Tooltip("ブレンドシェイプが100になる目標のankoCount")]
    private int targetAnkoCount = 7;

    [SerializeField, Tooltip("変更するブレンドシェイプの名前")]
    private string blendShapeName = "anko";

    // ブレンドシェイプのインデックスをキャッシュする変数
    private int blendShapeIndex = -1;

    private void Awake()
    {
        // ScoreManagerがアサインされていない場合は、アタッチされたGameObjectから取得
        if (scoreManager == null)
        {
            scoreManager = GetComponent<ScoreManager>();
        }

        // SkinnedMeshRendererがアサインされていない場合は、アタッチされたGameObjectから取得
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SkinnedMeshRenderer>();
        }

        // ブレンドシェイプのインデックスを検索して保存
        if (targetRenderer != null && targetRenderer.sharedMesh != null)
        {
            blendShapeIndex = targetRenderer.sharedMesh.GetBlendShapeIndex(blendShapeName);
            if (blendShapeIndex == -1)
            {
                Debug.LogWarning($"[AnkoBlendShapeController] {targetRenderer.gameObject.name} にブレンドシェイプ '{blendShapeName}' が見つかりません。", targetRenderer);
            }
        }
        else
        {
            Debug.LogWarning("[AnkoBlendShapeController] SkinnedMeshRendererが見つからないか、Meshが設定されていません。", this);
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
            Debug.LogWarning("[AnkoBlendShapeController] ScoreManagerが見つかりません。", this);
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
    /// ankoCountに応じて対象のブレンドシェイプを更新する
    /// </summary>
    private void UpdateBlendShapeWeights(int currentAnkoCount)
    {
        // 対象がない、またはブレンドシェイプが見つかっていない場合は処理しない
        if (targetRenderer == null || blendShapeIndex == -1) return;

        // 目標値に対する現在の取得数の割合を計算
        float progress = Mathf.Clamp01((float)currentAnkoCount / targetAnkoCount);

        // ブレンドシェイプのウェイト (0 ～ 100) を計算
        float weight = progress * 100f;

        // SkinnedMeshRendererに値を適用
        targetRenderer.SetBlendShapeWeight(blendShapeIndex, weight);
    }
}