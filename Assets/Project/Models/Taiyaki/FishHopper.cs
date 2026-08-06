using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 地面に触れるたびに自動で跳ねる魚を制御します。
/// 跳躍時には水平方向へランダムな偏りが加わり、空中入力で軌道を補正できます。
/// あんこの量が多いほど重く、低速で、安定した挙動になります。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class FishHopper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody body;
    [Tooltip("見た目だけを回転させる場合に指定します。未指定なら回転しません。")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Animator animator;

    [Header("Input")]
#if ENABLE_INPUT_SYSTEM
    [Tooltip("新Input SystemのInputActionAssetを設定します。")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
#endif
    [Tooltip("旧 Input Manager の Horizontal / Vertical 軸を読み取ります。新 Input System 専用設定では自動的に無視されます。")]
    [SerializeField] private bool readLegacyInput;
    [SerializeField, Range(0f, 1f)] private float inputDeadZone = 0.15f;

    [Header("Control / Random Balance")]
    [Tooltip("0 = 完全ランダム、1 = 完全にプレイヤー操作。1で無入力の場合は横移動せず、真上に跳ねます。")]
    [InspectorName("操作割合（残りはランダム）")]
    [SerializeField, Range(0f, 1f)] private float operationRatio = 0.55f;

    [Header("Hop")]
    [Min(0f)] [SerializeField] private float jumpSpeed = 6f;
    [Min(0f)] [SerializeField] private float horizontalHopSpeed = 3.5f;
    [Min(0f)] [SerializeField] private float airAcceleration = 12f;
    [Min(0f)] [SerializeField] private float maximumHorizontalSpeed = 5f;
    [Min(0f)] [SerializeField] private float minimumHopInterval = 0.06f;
    [Range(0f, 180f)] [SerializeField] private float maximumRandomAngle = 75f;

    [Header("Snappy Gravity")]
    [Tooltip("上昇中の重力倍率。1はUnity標準重力です。")]
    [Min(1f)] [SerializeField] private float risingGravityMultiplier = 1.7f;
    [Tooltip("上昇・下降速度の絶対値がこの値以下なら頂点付近と判定します。")]
    [Min(0f)] [SerializeField] private float apexVelocityThreshold = 1.2f;
    [Tooltip("頂点付近の重力倍率。大きくすると頂点で素早く折り返します。")]
    [Min(1f)] [SerializeField] private float apexGravityMultiplier = 3.4f;
    [Tooltip("下降中の重力倍率。上昇倍率より大きくすると素早く落下します。")]
    [Min(1f)] [SerializeField] private float fallingGravityMultiplier = 2.6f;

    [Header("Ground Detection")]
    [Tooltip("地面として扱うレイヤー。Everything のままだと他の魚も地面扱いする場合があります。")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [Range(0f, 1f)] [SerializeField] private float minimumGroundNormalY = 0.55f;

    [Header("Anko")]
    [Tooltip("0 = あんこなし、1 = あんこ最大")]
    [SerializeField, Range(0f, 1f)] private float ankoAmount;
    [Min(0.01f)] [SerializeField] private float emptyMass = 1f;
    [Min(0.01f)] [SerializeField] private float fullMass = 2.2f;
    [Range(0.1f, 1f)] [SerializeField] private float fullAnkoJumpMultiplier = 0.72f;
    [Range(0.1f, 1f)] [SerializeField] private float fullAnkoMoveMultiplier = 0.62f;
    [Range(0f, 1f)] [SerializeField] private float fullAnkoRandomMultiplier = 0.3f;
    [Range(0.1f, 1f)] [SerializeField] private float fullAnkoControlMultiplier = 0.65f;

    [Header("Visual")]
    [Min(0f)] [SerializeField] private float visualTurnSpeed = 540f;

    private Vector2 moveInput;
    private bool grounded;
    private float nextHopTime;
    private Vector3 lastHopDirection;
#if ENABLE_INPUT_SYSTEM
    private InputAction moveAction;
    private bool enabledMoveActionHere;
#endif

    public float AnkoAmount => ankoAmount;
    public bool IsGrounded => grounded;
    public float OperationRatio => operationRatio;
    public float RandomRatio => 1f - operationRatio;

    private void Reset()
    {
        body = GetComponent<Rigidbody>();
        ApplyFishLikeDefaults();
    }

    [ContextMenu("Apply Fish-like Defaults")]
    private void ApplyFishLikeDefaults()
    {
        inputDeadZone = 0.15f;
        operationRatio = 0.55f;

        jumpSpeed = 6f;
        horizontalHopSpeed = 3.5f;
        airAcceleration = 12f;
        maximumHorizontalSpeed = 5f;
        minimumHopInterval = 0.06f;
        maximumRandomAngle = 75f;
        risingGravityMultiplier = 1.7f;
        apexVelocityThreshold = 1.2f;
        apexGravityMultiplier = 3.4f;
        fallingGravityMultiplier = 2.6f;

        emptyMass = 1f;
        fullMass = 2.2f;
        fullAnkoJumpMultiplier = 0.72f;
        fullAnkoMoveMultiplier = 0.62f;
        fullAnkoRandomMultiplier = 0.3f;
        fullAnkoControlMultiplier = 0.65f;

        if (body != null)
        {
            ApplyAnkoMass();
        }
    }

    private void Awake()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        ApplyAnkoMass();
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        if (inputActions == null)
        {
            Debug.LogWarning("FishHopper: Input Actionsが設定されていません。", this);
            return;
        }

        InputActionMap actionMap = inputActions.FindActionMap(actionMapName, false);
        moveAction = actionMap?.FindAction(moveActionName, false);

        if (moveAction == null)
        {
            Debug.LogWarning(
                $"FishHopper: Input Action '{actionMapName}/{moveActionName}' が見つかりません。",
                this);
            return;
        }

        if (!moveAction.enabled)
        {
            moveAction.Enable();
            enabledMoveActionHere = true;
        }
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (enabledMoveActionHere && moveAction != null)
        {
            moveAction.Disable();
        }

        enabledMoveActionHere = false;
        moveAction = null;
#endif
    }

    private void OnValidate()
    {
        ankoAmount = Mathf.Clamp01(ankoAmount);
        operationRatio = Mathf.Clamp01(operationRatio);
        risingGravityMultiplier = Mathf.Max(1f, risingGravityMultiplier);
        apexVelocityThreshold = Mathf.Max(0f, apexVelocityThreshold);
        apexGravityMultiplier = Mathf.Max(1f, apexGravityMultiplier);
        fallingGravityMultiplier = Mathf.Max(1f, fallingGravityMultiplier);
        emptyMass = Mathf.Max(0.01f, emptyMass);
        fullMass = Mathf.Max(0.01f, fullMass);

        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        if (body != null)
        {
            ApplyAnkoMass();
        }
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (moveAction != null)
        {
            SetMoveInput(moveAction.ReadValue<Vector2>());
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (readLegacyInput)
        {
            SetMoveInput(new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")));
        }
#endif

        UpdateVisualDirection();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        ApplyAdditionalGravity();
        ApplyAirControl();

        if (grounded && Time.time >= nextHopTime)
        {
            Hop();
        }

        // 接触イベントを受けなかったフレームでは空中とみなします。
        grounded = false;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayers.value) == 0)
        {
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y >= minimumGroundNormalY)
            {
                grounded = true;
                return;
            }
        }
    }

    /// <summary>
    /// 新 Input System など外部の入力処理から、X/Z 平面の移動入力を渡します。
    /// </summary>
    public void SetMoveInput(Vector2 input)
    {
        moveInput = Vector2.ClampMagnitude(input, 1f);

        if (moveInput.sqrMagnitude < inputDeadZone * inputDeadZone)
        {
            moveInput = Vector2.zero;
        }
    }

    public void SetAnkoAmount(float amount)
    {
        ankoAmount = Mathf.Clamp01(amount);
        ApplyAnkoMass();
    }

    public void AddAnko(float amount)
    {
        SetAnkoAmount(ankoAmount + amount);
    }

    public void SetOperationRatio(float operation)
    {
        operationRatio = Mathf.Clamp01(operation);
    }

    private void Hop()
    {
        float jumpMultiplier = Mathf.Lerp(1f, fullAnkoJumpMultiplier, ankoAmount);
        float moveMultiplier = Mathf.Lerp(1f, fullAnkoMoveMultiplier, ankoAmount);
        float randomMultiplier = Mathf.Lerp(1f, fullAnkoRandomMultiplier, ankoAmount);

        Vector3 horizontalVelocity = GetHopHorizontalVelocity(moveMultiplier, randomMultiplier);

        Vector3 velocity = body.linearVelocity;
        velocity.y = jumpSpeed * jumpMultiplier;
        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;
        body.linearVelocity = velocity;

        if (horizontalVelocity.sqrMagnitude > 0.0001f)
        {
            lastHopDirection = horizontalVelocity.normalized;
        }

        grounded = false;
        nextHopTime = Time.time + minimumHopInterval;

        if (animator != null)
        {
            animator.SetTrigger("Hop");
        }
    }

    private Vector3 GetHopHorizontalVelocity(float moveMultiplier, float randomMultiplier)
    {
        float randomWeight = (1f - operationRatio) * randomMultiplier;
        Vector3 inputDirection = moveInput != Vector2.zero
            ? new Vector3(moveInput.x, 0f, moveInput.y).normalized
            : Vector3.zero;

        Vector3 referenceDirection = GetRandomReferenceDirection(inputDirection);
        float randomAngle = Random.Range(-maximumRandomAngle, maximumRandomAngle);
        Vector3 randomDirection = Quaternion.AngleAxis(randomAngle, Vector3.up)
            * referenceDirection;

        Vector3 operationPart = inputDirection * operationRatio;
        Vector3 randomPart = randomDirection * randomWeight;
        Vector3 combinedMovement = operationPart + randomPart;

        return combinedMovement * horizontalHopSpeed * moveMultiplier;
    }

    private Vector3 GetRandomReferenceDirection(Vector3 inputDirection)
    {
        if (inputDirection.sqrMagnitude > 0.0001f)
        {
            return inputDirection;
        }

        if (lastHopDirection.sqrMagnitude > 0.0001f)
        {
            return lastHopDirection.normalized;
        }

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
    }

    private void ApplyAirControl()
    {
        if (grounded || moveInput == Vector2.zero)
        {
            return;
        }

        float controlMultiplier = Mathf.Lerp(1f, fullAnkoControlMultiplier, ankoAmount);
        float speedMultiplier = Mathf.Lerp(1f, fullAnkoMoveMultiplier, ankoAmount);
        Vector3 desiredDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 acceleration = desiredDirection
            * airAcceleration
            * controlMultiplier
            * operationRatio;
        body.AddForce(acceleration, ForceMode.Acceleration);

        Vector3 velocity = body.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float speedLimit = maximumHorizontalSpeed * speedMultiplier;

        if (horizontalVelocity.sqrMagnitude > speedLimit * speedLimit)
        {
            horizontalVelocity = horizontalVelocity.normalized * speedLimit;
            body.linearVelocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
        }
    }

    private void ApplyAdditionalGravity()
    {
        if (grounded || !body.useGravity)
        {
            return;
        }

        float verticalSpeed = body.linearVelocity.y;
        float gravityMultiplier;

        if (Mathf.Abs(verticalSpeed) <= apexVelocityThreshold)
        {
            gravityMultiplier = apexGravityMultiplier;
        }
        else
        {
            gravityMultiplier = verticalSpeed > 0f
                ? risingGravityMultiplier
                : fallingGravityMultiplier;
        }

        body.AddForce(
            Physics.gravity * (gravityMultiplier - 1f),
            ForceMode.Acceleration);
    }

    private void ApplyAnkoMass()
    {
        body.mass = Mathf.Lerp(emptyMass, fullMass, ankoAmount);
    }

    private void UpdateVisualDirection()
    {
        if (visualRoot == null)
        {
            return;
        }

        Vector3 horizontalVelocity = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
        if (horizontalVelocity.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity.normalized, Vector3.up);
        visualRoot.rotation = Quaternion.RotateTowards(
            visualRoot.rotation,
            targetRotation,
            visualTurnSpeed * Time.deltaTime);
    }

    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        Vector3 horizontalVelocity = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
        animator.SetBool("Grounded", grounded);
        animator.SetFloat("VerticalSpeed", body.linearVelocity.y);
        animator.SetFloat("HorizontalSpeed", horizontalVelocity.magnitude);
        animator.SetFloat("AnkoAmount", ankoAmount);
    }
}
