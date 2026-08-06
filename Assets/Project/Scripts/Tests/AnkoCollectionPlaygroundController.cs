using UnityEngine;
using UnityEngine.InputSystem;

namespace TaiyakiKun.Tests
{
    /// <summary>
    /// Provides keyboard movement and a small debug overlay for the collection playground.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ScoreManager))]
    public sealed class AnkoCollectionPlaygroundController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField]
        [Min(0f)]
        private float moveSpeed = 4f;

        [SerializeField]
        [Min(0f)]
        private float rotationSpeed = 12f;

        [SerializeField]
        private Vector2 horizontalBounds = new Vector2(-4.2f, 4.2f);

        [SerializeField]
        private Vector2 depthBounds = new Vector2(-4.2f, 4.2f);

        [SerializeField]
        [Min(0.1f)]
        private float pickupPopupDuration = 0.8f;

        [SerializeField]
        [Min(0f)]
        private float pickupPopupRise = 36f;

        private Rigidbody body;
        private ScoreManager scoreManager;
        private Camera gameplayCamera;
        private Vector2 moveInput;
        private int totalAnkoCount;
        private int previousAnkoCount;
        private int popupAmount;
        private float popupStartedAt = float.NegativeInfinity;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            scoreManager = GetComponent<ScoreManager>();
            previousAnkoCount = scoreManager.AnkoCount;
            ApplyTestCubeColor();
        }

        private void OnEnable()
        {
            scoreManager.AnkoCountChanged += HandleAnkoCountChanged;
        }

        private void OnDisable()
        {
            scoreManager.AnkoCountChanged -= HandleAnkoCountChanged;
        }

        private void Start()
        {
            totalAnkoCount = FindObjectsByType<AnkoCollectible>(FindObjectsSortMode.None).Length;
            gameplayCamera = Camera.main;
        }

        private void Update()
        {
            ReadKeyboardInput();
        }

        private void FixedUpdate()
        {
            Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            direction.Normalize();
            Vector3 nextPosition = body.position + direction * (moveSpeed * Time.fixedDeltaTime);
            nextPosition.x = Mathf.Clamp(nextPosition.x, horizontalBounds.x, horizontalBounds.y);
            nextPosition.z = Mathf.Clamp(nextPosition.z, depthBounds.x, depthBounds.y);

            body.MovePosition(nextPosition);

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            Quaternion nextRotation = Quaternion.Slerp(
                body.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime);
            body.MoveRotation(nextRotation);
        }

        private void OnGUI()
        {
            const float panelWidth = 340f;
            const float panelHeight = 96f;
            Rect panel = new Rect(18f, 18f, panelWidth, panelHeight);

            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = previousColor;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 20;
            labelStyle.normal.textColor = Color.white;

            GUI.Label(new Rect(34f, 28f, 310f, 28f), "Move: WASD / Arrow Keys", labelStyle);
            GUI.Label(
                new Rect(34f, 58f, 310f, 28f),
                $"Anko: {scoreManager.AnkoCount} / {totalAnkoCount}",
                labelStyle);

            DrawPickupPopup();
        }

        private void HandleAnkoCountChanged(int newCount)
        {
            int collectedAmount = newCount - previousAnkoCount;
            previousAnkoCount = newCount;

            if (collectedAmount <= 0)
            {
                return;
            }

            popupAmount = collectedAmount;
            popupStartedAt = Time.time;
        }

        private void DrawPickupPopup()
        {
            float elapsed = Time.time - popupStartedAt;
            if (popupAmount <= 0 || elapsed < 0f || elapsed >= pickupPopupDuration)
            {
                return;
            }

            Camera targetCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
            if (targetCamera == null)
            {
                return;
            }

            Vector3 screenPosition = targetCamera.WorldToScreenPoint(
                transform.position + Vector3.up * 1.25f);
            if (screenPosition.z <= 0f)
            {
                return;
            }

            float progress = Mathf.Clamp01(elapsed / pickupPopupDuration);
            float guiY = Screen.height - screenPosition.y - pickupPopupRise * progress;
            Rect popupRect = new Rect(screenPosition.x - 60f, guiY - 25f, 120f, 50f);

            GUIStyle popupStyle = new GUIStyle(GUI.skin.label);
            popupStyle.fontSize = 30;
            popupStyle.fontStyle = FontStyle.Bold;
            popupStyle.alignment = TextAnchor.MiddleCenter;
            popupStyle.normal.textColor = new Color(1f, 0.83f, 0.12f, 1f - progress);

            GUIStyle shadowStyle = new GUIStyle(popupStyle);
            shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.65f * (1f - progress));

            string popupText = $"+{popupAmount}";
            Rect shadowRect = new Rect(popupRect.x + 2f, popupRect.y + 2f, popupRect.width, popupRect.height);
            GUI.Label(shadowRect, popupText, shadowStyle);
            GUI.Label(popupRect, popupText, popupStyle);
        }

        private void ReadKeyboardInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                moveInput = Vector2.zero;
                return;
            }

            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                horizontal -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                horizontal += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                vertical -= 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                vertical += 1f;
            }

            moveInput = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        }

        private void ApplyTestCubeColor()
        {
            Renderer cubeRenderer = GetComponent<Renderer>();
            if (cubeRenderer == null)
            {
                return;
            }

            Color cubeColor = new Color(1f, 0.55f, 0.12f);
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            cubeRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, cubeColor);
            propertyBlock.SetColor(ColorId, cubeColor);
            cubeRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
