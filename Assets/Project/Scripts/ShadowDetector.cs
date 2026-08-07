using UnityEngine;

public class ShadowDetector : MonoBehaviour
{
    [Header("設定項目")]
    public Light mainLight;
    public LayerMask obstacleLayer;
    public Vector3 raycastOffset = new Vector3(0, 0.5f, 0);
    public FishHopper fishHopper;

    [Header("判定結果 (読み取り専用)")]
    public bool isInShadow = false;

    private void Start()
    {
        CheckShadow();
    }

    private void Update()
    {
        CheckShadow();
    }

    private void CheckShadow()
    {
        if (fishHopper != null && !fishHopper.IsGrounded)
        {
            isInShadow = true;
            return;
        }

        if (mainLight == null || mainLight.type != LightType.Directional)
        {
            Debug.LogWarning("Directional Lightがアタッチされていないか、タイプが異なります。");
            return;
        }

        Vector3 lightDir = -mainLight.transform.forward;
        Vector3 startPos = transform.position + raycastOffset;

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