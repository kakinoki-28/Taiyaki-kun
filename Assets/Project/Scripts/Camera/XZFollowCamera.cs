using UnityEngine;

/// <summary>
/// 斜め45度の俯瞰視点を保ちながら、ターゲットのX/Z位置だけを追従します。
/// カメラのY座標（高さ）は固定されます。
/// </summary>
[DisallowMultipleComponent]
public sealed class XZFollowCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("XZ Follow")]
    [Tooltip("有効にすると、再生開始時のカメラとターゲットのX/Z間隔を維持します。")]
    [SerializeField] private bool useInitialXZOffset;
    [Tooltip("ターゲットから見たカメラのX/Z位置。初期値はターゲットの後方10です。")]
    [SerializeField] private Vector2 xzOffset = new Vector2(0f, -10f);
    [Tooltip("0なら即座に追従します。値を大きくすると滑らかになります。")]
    [Min(0f)] [SerializeField] private float smoothTime = 0.15f;

    [Header("Fixed Y")]
    [Tooltip("有効にすると、再生開始時のカメラのY座標を固定値として使用します。")]
    [SerializeField] private bool useInitialY;
    [Tooltip("追従中に変化しないカメラの高さです。")]
    [SerializeField] private float fixedY = 10f;

    [Header("Fixed View")]
    [Tooltip("X=45で、斜め上45度から+Z方向を見る俯瞰視点になります。")]
    [SerializeField] private Vector3 fixedEulerAngles = new Vector3(45f, 0f, 0f);

    private Vector2 followVelocity;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("XZFollowCamera: Targetが設定されていません。", this);
            enabled = false;
            return;
        }

        if (useInitialXZOffset)
        {
            xzOffset = new Vector2(
                transform.position.x - target.position.x,
                transform.position.z - target.position.z);
        }

        if (useInitialY)
        {
            fixedY = transform.position.y;
        }

        ApplyFixedTransform(false);
    }

    private void LateUpdate()
    {
        ApplyFixedTransform(true);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        followVelocity = Vector2.zero;
        enabled = target != null;
    }

    private void ApplyFixedTransform(bool smooth)
    {
        if (target == null)
        {
            return;
        }

        Vector2 desiredXZ = new Vector2(
            target.position.x + xzOffset.x,
            target.position.z + xzOffset.y);

        Vector2 currentXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 nextXZ = !smooth || smoothTime <= 0f
            ? desiredXZ
            : Vector2.SmoothDamp(
                currentXZ,
                desiredXZ,
                ref followVelocity,
                smoothTime,
                Mathf.Infinity,
                Time.deltaTime);

        transform.SetPositionAndRotation(
            new Vector3(nextXZ.x, fixedY, nextXZ.y),
            Quaternion.Euler(fixedEulerAngles));
    }

    private void OnValidate()
    {
        smoothTime = Mathf.Max(0f, smoothTime);
    }
}
