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
        private Vector2 horizontalBounds = new Vector2(-15f, 15f);

        [SerializeField]
        private Vector2 depthBounds = new Vector2(0f, 100f);

        [SerializeField]
        [Min(0.1f)]
        private float pickupPopupDuration = 0.8f;

        [SerializeField]
        [Min(0f)]
        private float pickupPopupRise = 36f;

        [SerializeField]
        private global::FishHopper fishHopper;

        [SerializeField]
        private AudioClip pickupSound;

        [SerializeField]
        [Range(0f, 1f)]
        private float pickupSoundVolume = 0.85f;

        private Rigidbody body;
        private ScoreManager scoreManager;
        private global::Sunburn sunburn;
        private CollectionFeedbackRelay feedbackRelay;
        private AudioSource pickupAudioSource;
        private Camera gameplayCamera;
        private Vector2 moveInput;
        private int totalAnkoCount;
        private int previousAnkoCount;
        private string popupText;
        private Color popupColor = Color.white;
        private float popupStartedAt = float.NegativeInfinity;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            scoreManager = GetComponent<ScoreManager>();
            sunburn = GetComponent<global::Sunburn>();
            feedbackRelay = GetComponent<CollectionFeedbackRelay>();
            if (feedbackRelay == null)
            {
                feedbackRelay = gameObject.AddComponent<CollectionFeedbackRelay>();
            }

            fishHopper = fishHopper != null ? fishHopper : GetComponent<global::FishHopper>();
            pickupAudioSource = GetComponent<AudioSource>();
            if (pickupAudioSource == null)
            {
                pickupAudioSource = gameObject.AddComponent<AudioSource>();
            }

            pickupAudioSource.playOnAwake = false;
            pickupAudioSource.loop = false;
            pickupAudioSource.spatialBlend = 0f;
            previousAnkoCount = scoreManager.AnkoCount;
            ApplyTestCubeColor();
        }

        private void OnEnable()
        {
            scoreManager.AnkoCountChanged += HandleAnkoCountChanged;
            feedbackRelay.FeedbackRequested += HandleFeedbackRequested;
        }

        private void OnDisable()
        {
            scoreManager.AnkoCountChanged -= HandleAnkoCountChanged;
            feedbackRelay.FeedbackRequested -= HandleFeedbackRequested;

            if (fishHopper != null)
            {
                fishHopper.SetMoveInput(Vector2.zero);
            }
        }

        private void Start()
        {
            totalAnkoCount = FindObjectsByType<AnkoCollectible>(FindObjectsSortMode.None).Length;
            gameplayCamera = Camera.main;
        }

        private void Update()
        {
            ReadKeyboardInput();

            if (fishHopper != null)
            {
                fishHopper.SetMoveInput(moveInput);
            }
        }

        private void FixedUpdate()
        {
            if (fishHopper != null)
            {
                KeepHopperInsideArena();
                return;
            }

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

        private void KeepHopperInsideArena()
        {
            Vector3 position = body.position;
            float clampedX = Mathf.Clamp(position.x, horizontalBounds.x, horizontalBounds.y);
            float clampedZ = Mathf.Clamp(position.z, depthBounds.x, depthBounds.y);
            if (Mathf.Approximately(position.x, clampedX)
                && Mathf.Approximately(position.z, clampedZ))
            {
                return;
            }

            Vector3 velocity = body.linearVelocity;
            if (!Mathf.Approximately(position.x, clampedX))
            {
                velocity.x = 0f;
            }

            if (!Mathf.Approximately(position.z, clampedZ))
            {
                velocity.z = 0f;
            }

            body.position = new Vector3(clampedX, position.y, clampedZ);
            body.linearVelocity = velocity;
        }

        private void OnGUI()
        {
            const float panelWidth = 390f;
            const float panelHeight = 126f;
            Rect panel = new Rect(18f, 18f, panelWidth, panelHeight);

            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = previousColor;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 20;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.normal.textColor = Color.white;

            float health = sunburn != null ? sunburn.SunburnHealthNormalized : 1f;
            GUI.Label(new Rect(34f, 31f, 82f, 30f), "日焼け", labelStyle);
            DrawSunburnBar(new Rect(112f, 33f, 274f, 27f), health, labelStyle);

            GUIStyle ankoStyle = new GUIStyle(labelStyle);
            ankoStyle.fontSize = 24;
            GUI.Label(
                new Rect(34f, 79f, 340f, 34f),
                $"あんこ量  {scoreManager.AnkoCount * 100}g",
                ankoStyle);

            DrawPickupPopup();
        }

        private static void DrawSunburnBar(Rect barRect, float health, GUIStyle labelStyle)
        {
            health = Mathf.Clamp01(health);
            DrawSolidRect(barRect, new Color(0.92f, 0.92f, 0.92f, 1f));

            Rect backgroundRect = new Rect(
                barRect.x + 3f,
                barRect.y + 3f,
                barRect.width - 6f,
                barRect.height - 6f);
            DrawSolidRect(backgroundRect, new Color(0.12f, 0.08f, 0.08f, 0.95f));

            Rect fillRect = backgroundRect;
            fillRect.width *= health;
            Color lowColor = new Color(0.95f, 0.16f, 0.08f);
            Color middleColor = new Color(1f, 0.72f, 0.08f);
            Color highColor = new Color(0.2f, 0.9f, 0.3f);
            Color fillColor = health < 0.5f
                ? Color.Lerp(lowColor, middleColor, health * 2f)
                : Color.Lerp(middleColor, highColor, (health - 0.5f) * 2f);
            DrawSolidRect(fillRect, fillColor);

            GUIStyle barLabelStyle = new GUIStyle(labelStyle);
            barLabelStyle.fontSize = 16;
            barLabelStyle.alignment = TextAnchor.MiddleCenter;
            string barText = health >= 0.999f
                ? "MAX"
                : $"{Mathf.RoundToInt(health * 100f)}%";
            GUI.Label(barRect, barText, barLabelStyle);
        }

        private static void DrawSolidRect(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void HandleAnkoCountChanged(int newCount)
        {
            int collectedAmount = newCount - previousAnkoCount;
            previousAnkoCount = newCount;

            if (collectedAmount <= 0)
            {
                return;
            }

            if (fishHopper != null)
            {
                float normalizedAnkoAmount = totalAnkoCount > 0
                    ? (float)newCount / totalAnkoCount
                    : 0f;
                fishHopper.SetAnkoAmount(normalizedAnkoAmount);
            }

            ShowPickupPopup($"+{collectedAmount}", new Color(1f, 0.83f, 0.12f));
            PlayPickupSound();
        }

        private void HandleFeedbackRequested(string message, Color color)
        {
            ShowPickupPopup(message, color);
            PlayPickupSound();
        }

        private void ShowPickupPopup(string message, Color color)
        {
            popupText = message;
            popupColor = color;
            popupStartedAt = Time.time;
        }

        private void PlayPickupSound()
        {
            if (pickupSound != null)
            {
                pickupAudioSource.PlayOneShot(pickupSound, pickupSoundVolume);
            }
        }

        private void DrawPickupPopup()
        {
            float elapsed = Time.time - popupStartedAt;
            if (string.IsNullOrEmpty(popupText)
                || elapsed < 0f
                || elapsed >= pickupPopupDuration)
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
            Rect popupRect = new Rect(screenPosition.x - 110f, guiY - 25f, 220f, 50f);

            GUIStyle popupStyle = new GUIStyle(GUI.skin.label);
            popupStyle.fontSize = 30;
            popupStyle.fontStyle = FontStyle.Bold;
            popupStyle.alignment = TextAnchor.MiddleCenter;
            popupStyle.normal.textColor = new Color(
                popupColor.r,
                popupColor.g,
                popupColor.b,
                popupColor.a * (1f - progress));

            GUIStyle shadowStyle = new GUIStyle(popupStyle);
            shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.65f * (1f - progress));

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
