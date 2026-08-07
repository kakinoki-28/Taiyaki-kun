using UnityEngine;

public class ShadowDetector : MonoBehaviour
{
    [Header("設定項目")]
    [Tooltip("太陽となる光源（Directional Light）を指定してください")]
    public Light mainLight;

    [Tooltip("影を作る（光を遮る）オブジェクトのレイヤーを指定します。自身のレイヤーは外してください。")]
    public LayerMask obstacleLayer;

    [Tooltip("レイを飛ばす開始位置の高さオフセット（足元ではなく、胸や頭の高さで判定したい場合に調整します）")]
    public Vector3 raycastOffset = new Vector3(0, 0.5f, 0);

    [Tooltip("ジャンプ状態を取得するためのFishHopperを指定してください")]
    public FishHopper fishHopper;

    [Header("判定結果 (読み取り専用)")]
    public bool isInShadow = false;

    private void Update()
    {
        CheckShadow();
    }

    private void CheckShadow()
    {
        // 空中場合
        if (fishHopper != null && !fishHopper.IsGrounded)
        {
            isInShadow = true;
            return;
        }
        // ----------------

        if (mainLight == null || mainLight.type != LightType.Directional)
        {
            Debug.LogWarning("Directional Lightがアタッチされていないか、タイプが異なります。");
            return;
        }

        // 光源の逆方向
        Vector3 lightDir = -mainLight.transform.forward;

        // 判定の開始位置
        Vector3 startPos = transform.position + raycastOffset;

        // 光源に向かってレイキャスト
        if (Physics.Raycast(startPos, lightDir, out RaycastHit hit, Mathf.Infinity, obstacleLayer))
        {
            isInShadow = true;
            Debug.DrawRay(startPos, lightDir * hit.distance, Color.red);
        }
        else
        {
            isInShadow = false;
            Debug.DrawRay(startPos, lightDir * 10f, Color.yellow);
        }
    }
}