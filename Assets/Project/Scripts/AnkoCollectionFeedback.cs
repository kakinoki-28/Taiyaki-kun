using UnityEngine;
using UnityEngine.UI;

namespace TaiyakiKun
{
    /// <summary>
    /// あんこ画像はワールド空間に保ち、取得数と取得時の「+N」をOverlayへ表示します。
    /// 移動処理を持たないためFishHopperと併用できます。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScoreManager))]
    public sealed class AnkoCollectionFeedback : MonoBehaviour
    {
        [Header("Counter")]
        [SerializeField] private bool showCounter = true;

        [Header("Pickup Popup")]
        [SerializeField] private Vector3 popupWorldOffset = new Vector3(0f, 1.25f, 0f);
        [SerializeField] [Min(0.1f)] private float popupDuration = 0.8f;
        [SerializeField] [Min(0f)] private float popupRise = 36f;

        private ScoreManager scoreManager;
        private Camera gameplayCamera;
        private Canvas overlayCanvas;
        private Text counterText;
        private Text popupText;
        private RectTransform popupRectTransform;
        private int totalAnkoCount;
        private int previousAnkoCount;
        private int popupAmount;
        private float popupStartedAt = float.NegativeInfinity;

        private void Awake()
        {
            scoreManager = GetComponent<ScoreManager>();
            previousAnkoCount = scoreManager.AnkoCount;
        }

        private void OnEnable()
        {
            scoreManager.AnkoCountChanged += HandleAnkoCountChanged;
        }

        private void OnDisable()
        {
            if (scoreManager != null)
            {
                scoreManager.AnkoCountChanged -= HandleAnkoCountChanged;
            }
        }

        private void Start()
        {
            gameplayCamera = Camera.main;
            CreateOverlayCanvas();
            PrepareWorldBillboards();
            UpdateCounter();
        }

        private void Update()
        {
            UpdatePickupPopup();
        }

        private void OnDestroy()
        {
            if (overlayCanvas != null)
            {
                Destroy(overlayCanvas.gameObject);
            }
        }

        private void OnValidate()
        {
            popupDuration = Mathf.Max(0.1f, popupDuration);
            popupRise = Mathf.Max(0f, popupRise);
        }

        private void HandleAnkoCountChanged(int newCount)
        {
            int collectedAmount = newCount - previousAnkoCount;
            previousAnkoCount = newCount;
            UpdateCounter();

            if (collectedAmount <= 0)
            {
                return;
            }

            popupAmount = collectedAmount;
            popupStartedAt = Time.unscaledTime;

            if (popupText != null)
            {
                popupText.text = $"+{popupAmount}";
                popupText.gameObject.SetActive(true);
            }
        }

        private void CreateOverlayCanvas()
        {
            GameObject canvasObject = new GameObject("Anko Collection Overlay");
            overlayCanvas = canvasObject.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 100;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject counterObject = new GameObject("Anko Counter");
            counterObject.transform.SetParent(canvasObject.transform, false);
            counterText = counterObject.AddComponent<Text>();
            counterText.font = font;
            counterText.fontSize = 24;
            counterText.fontStyle = FontStyle.Bold;
            counterText.alignment = TextAnchor.UpperLeft;
            counterText.color = Color.white;
            counterText.raycastTarget = false;
            Outline counterOutline = counterObject.AddComponent<Outline>();
            counterOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            counterOutline.effectDistance = new Vector2(2f, -2f);

            RectTransform counterRect = counterText.rectTransform;
            counterRect.anchorMin = new Vector2(0f, 1f);
            counterRect.anchorMax = new Vector2(0f, 1f);
            counterRect.pivot = new Vector2(0f, 1f);
            counterRect.anchoredPosition = new Vector2(20f, -18f);
            counterRect.sizeDelta = new Vector2(340f, 44f);
            counterObject.SetActive(showCounter);

            GameObject popupObject = new GameObject("Anko Pickup Popup");
            popupObject.transform.SetParent(canvasObject.transform, false);
            popupText = popupObject.AddComponent<Text>();
            popupText.font = font;
            popupText.fontSize = 34;
            popupText.fontStyle = FontStyle.Bold;
            popupText.alignment = TextAnchor.MiddleCenter;
            popupText.color = new Color(1f, 0.83f, 0.12f, 1f);
            popupText.raycastTarget = false;
            Outline popupOutline = popupObject.AddComponent<Outline>();
            popupOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            popupOutline.effectDistance = new Vector2(2f, -2f);

            popupRectTransform = popupText.rectTransform;
            popupRectTransform.sizeDelta = new Vector2(140f, 60f);
            popupObject.SetActive(false);
        }

        private void PrepareWorldBillboards()
        {
            AnkoCollectible[] collectibles = FindObjectsByType<AnkoCollectible>(
                FindObjectsSortMode.None);
            totalAnkoCount = collectibles.Length;

            foreach (AnkoCollectible collectible in collectibles)
            {
                SpriteRenderer spriteRenderer = collectible.GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    continue;
                }

                // Keep the collectible image in world space. This allows opaque
                // boxes and buildings in front of it to occlude it normally.
                spriteRenderer.enabled = true;
                spriteRenderer.sortingOrder = 0;
            }
        }

        private void UpdateCounter()
        {
            if (counterText != null)
            {
                counterText.text = $"Anko: {scoreManager.AnkoCount} / {totalAnkoCount}";
            }
        }

        private void UpdatePickupPopup()
        {
            if (popupText == null || !popupText.gameObject.activeSelf)
            {
                return;
            }

            float elapsed = Time.unscaledTime - popupStartedAt;
            if (elapsed < 0f || elapsed >= popupDuration)
            {
                popupText.gameObject.SetActive(false);
                return;
            }

            Camera targetCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
            if (targetCamera == null)
            {
                popupText.gameObject.SetActive(false);
                return;
            }

            Vector3 screenPosition = targetCamera.WorldToScreenPoint(
                transform.position + popupWorldOffset);
            if (screenPosition.z <= 0f)
            {
                popupText.gameObject.SetActive(false);
                return;
            }

            float progress = Mathf.Clamp01(elapsed / popupDuration);
            popupRectTransform.position = screenPosition + Vector3.up * (popupRise * progress);
            Color color = popupText.color;
            color.a = 1f - progress;
            popupText.color = color;
        }
    }
}
